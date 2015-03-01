using PorygonOS.Core.Debug;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PorygonOS.Core.Tasks
{
    public class TaskScheduler
    {
        public TaskScheduler(int threadCount)
        {
            Setup(threadCount);
        }

        public TaskScheduler()
        {
            Setup(Environment.ProcessorCount);
        }

        public void Schedule(DateTime time, Task task)
        {
            /*Assume that if the time is less than the current time then we will just use the current time*/
            DateTime now = DateTime.Now;
            time = time < now ? now : time;

            lock(timeline)
            {
                timeline.Add(time, task);
            }

            if (bIsSleeping)
                managerThread.Interrupt();
        }

        public void EnterSafeMode()
        {
            lock (taskThreads)
            {
                foreach (TaskThread thread in taskThreads)
                {
                    thread.ActivateSafeMode();
                }

                foreach (TaskThread thread in taskThreads)
                {
                    while (!thread.IsInSafeMode) { }
                }
            }
        }

        public void LeaveSafeMode()
        {
            lock (taskThreads)
            {
                foreach (TaskThread thread in taskThreads)
                {
                    thread.WakeFromSafeMode();
                }

                foreach (TaskThread thread in taskThreads)
                {
                    while (thread.IsInSafeMode) { }
                }
            }
        }

        /// <summary>
        /// Pauses the scheduling thread.
        /// This does not suspend the scheduler thread, rather puts it into a sleep state when safe.
        /// This ensures that data is in a safe state when pause is finished, but may cause 
        /// </summary>
        public void Pause()
        {
            if (bShouldShutdown)
                return;

            lock(timeline)//we do not need a lock on this object, however it allows us not to enter the critical zone without knowing if the manager is asleep
            {
                if (bPauseUpdating)//don't try and pause if we are already in a paused state
                    return;

                bPauseUpdating = true;

                //Wake the manager thread if it is asleep
                if (bIsSleeping)
                {
                    managerThread.Interrupt();
                }
            }

            Thread.Yield();//let other threads get some processing time

            while (!bIsSleeping) { Thread.Yield(); }//wait for sleep to be re-signaled (this should be quick)
        }

        /// <summary>
        /// Unpauses the schduling thread.
        /// This will wait until all un-pausing is done until returning.
        /// </summary>
        public void UnPause()
        {
            if (bShouldShutdown)
                return;

            lock(timeline)
            {
                if (!bPauseUpdating)//if we are not paused do nothing
                    return;
                
                bPauseUpdating = false;

                if(bIsSleeping)
                {
                    managerThread.Interrupt();
                }
            }

            while (bIsSleeping) { Thread.Yield(); }
        }

        public void Shutdown()
        {
            bShouldShutdown = true;
            lock(timeline)
            {
                if (bIsSleeping)
                    managerThread.Interrupt();
            }
        }

        public void WaitForShutdownComplete()
        {
            while(!bIsShutdown)
            {
                Thread.Yield();
            }
        }

        public void SaveToBoot(string filePath)
        {
            Pause();

            FileStream saveStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(saveStream);
            writer.Write((byte)0);//allow for multiple versions of the boot file

            int taskCount = timeline.Count;
            int parity = 0;
            writer.Write(taskCount);
            for(int i = 0; i < taskCount; i++)
            {
                Type taskType = timeline.Values[i].GetType();
                if (taskType.IsDefined(typeof(NoBoot), true))
                    continue;

                writer.Write(taskType.FullName);

                DateTime scheduledTime = timeline.Keys[i];
                writer.Write(scheduledTime.ToLongDateString());

                bool shouldSerialize = !taskType.IsDefined(typeof(NonSerializedAttribute), true);
                writer.Write(shouldSerialize);
                if (!shouldSerialize)
                    continue;

                timeline.Values[i].Serialize(writer);

                writer.Write(parity);
                parity++;
            }

            writer.Close();

            UnPause();
        }

        public void LoadFromBoot(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);
            if (reader.ReadByte() != 0)
            {
                reader.Close();
                return;//only version 0 exists, any other version we will consider invalid for now
            }

            int taskCount = reader.ReadInt32();
            for(int i = 0; i < taskCount; i++)
            {
                string taskTypeName = reader.ReadString();
                Type taskType = Type.GetType(taskTypeName);
                if (taskType == null)
                    break;

                DateTime time = DateTime.Parse(reader.ReadString());

                bool canSerialize = reader.ReadBoolean();

                Task task;
                if (canSerialize)
                    task = FormatterServices.GetUninitializedObject(taskType) as Task;
                else
                    task = Activator.CreateInstance(taskType) as Task;

                if (task == null)
                    break;

                task.Deserialize(reader);

                int parity = reader.ReadInt32();
                if (parity != i)
                    break;

                Schedule(time, task);
            }

            reader.Close();
        }

        private void RunManager()
        {
            while (!bShouldShutdown)
            {
                WaitForNextTask();
                OptimizeTaskThreads();
                QueueTasks();
            }

            //signal every thread to shutdown
            foreach(TaskThread thread in taskThreads)
            {
                thread.Shutdown();
            }

            foreach(TaskThread thread in taskThreads)
            {
                while (thread.State != TaskThread.ThreadState.Dead) { Thread.Sleep(1000); }
            }

            SaveToBoot("Auto.boot");

            bIsShutdown = true;
        }

        private void WaitForNextTask()
        {

            /*Assuming no new tasks are added, we can have this thread sleep until a new task is scheduled.
             When a new task is schduled then an interrupt is fired, making the thread re-check its tasks.*/
            TimeSpan waitTime;
            lock (timeline)
            {
                if (bPauseUpdating || timeline.Count == 0)
                    waitTime = Timeout.InfiniteTimeSpan;
                else
                    waitTime = timeline.Keys[0] - DateTime.Now;

                if (waitTime.TotalMilliseconds <= 0.000001f && waitTime.TotalMilliseconds >= 0f)
                    return;//if we have a small period to wait, it is more efficient to not sleep and avoid the context switch

                bIsSleeping = true;
            }

            try
            {
                Thread.Sleep(waitTime);
            }
            catch (ThreadInterruptedException) { }
            catch (ArgumentOutOfRangeException) { }//If a negative time was supplied, just run the task.

            bIsSleeping = false;
        }

        private void QueueTasks()
        {
            lock(timeline)
            {
                while(timeline.Count > 0)
                {
                    DateTime nextTime = timeline.Keys[0];
                    DateTime now = DateTime.Now;
                    if (now < nextTime)
                        return;

                    OptimizeTaskThreads();//update the optimization list

                    Task nextTask = timeline.Values[0];
                    optimalTaskThreadList.Values[0].AddTask(nextTask);
                    timeline.RemoveAt(0);
                }
            }
        }

        private void OptimizeTaskThreads()
        {
            optimizeStateList.Clear();
            optimizeSleepTimeList.Clear();
            optimizeCycleTimeList.Clear();
            optimizeQueueCountList.Clear();
            optimizationCache.Clear();

            lock (optimalTaskThreadList)
            {
                optimalTaskThreadList.Clear();

                int taskCount;
                lock (taskThreads)
                {
                    taskCount = taskThreads.Count;
                    foreach(TaskThread thread in taskThreads)
                    {
                        if(thread.IsEmpty)
                        {
                            optimalTaskThreadList.Add(float.MinValue, thread);
                            continue;
                        }

                        optimizeStateList.Add(thread.State, thread);
                        optimizeSleepTimeList.Add(thread.LastSleepTime, thread);
                        optimizeCycleTimeList.Add(thread.AverageCycleTime, thread);
                        optimizeQueueCountList.Add(thread.TasksQueued, thread);

                        optimizationCache.Add(new Tuple<float, TaskThread>(0, thread));
                    }
                }

                int optimalCacheSize = optimizationCache.Count;
                for (int i = 0; i < optimalCacheSize; i++ )
                {
                    Tuple<float, TaskThread> op_info = optimizationCache[i]; ;

                    TaskThread taskThread = op_info.Item2;
                    float stateScore = (float)optimizeStateList.IndexOfValue(taskThread) * stateListWeight;
                    float sleepScore = (float)optimizeSleepTimeList.IndexOfValue(taskThread) * stateListWeight;
                    float cycleScore = (float)optimizeCycleTimeList.IndexOfValue(taskThread) * cycleListWeight;
                    float queueCountScore = (float)optimizeSleepTimeList.IndexOfValue(taskThread) * stateListWeight;

                    optimizationCache[i] = new Tuple<float,TaskThread>(stateScore + cycleScore + sleepScore + queueCountScore, taskThread);
                }

            
                
                foreach(Tuple<float, TaskThread> op_info in optimizationCache)
                {
                    optimalTaskThreadList.Add(op_info.Item1, op_info.Item2);
                }
            }
        }
        private SortedList<TaskThread.ThreadState, TaskThread> optimizeStateList = new SortedList<TaskThread.ThreadState, TaskThread>(Comparer<TaskThread.ThreadState>.Create((x, y) =>
        {
            int value = Comparer<TaskThread.ThreadState>.Default.Compare(y, x);
            return value == 0 ? 1 : value;
        }));
        private SortedList<DateTime, TaskThread> optimizeSleepTimeList = new SortedList<DateTime, TaskThread>(Comparer<DateTime>.Create((x,y)=>
        {
            int value = Comparer<DateTime>.Default.Compare(y, x);
            return value == 0 ? 1 : value;
        }));
        private SortedList<float, TaskThread> optimizeCycleTimeList = new SortedList<float, TaskThread>(Comparer<float>.Create((x, y) =>
        {
            int value = Math.Sign(x - y);
            return value == 0 ? 1 : value;
        }));
        private SortedList<int, TaskThread> optimizeQueueCountList = new SortedList<int, TaskThread>(Comparer<int>.Create((x, y) =>
        {
            int value = x - y;
            return value == 0 ? 1 : value;
        }));
        private List<Tuple<float, TaskThread>> optimizationCache = new List<Tuple<float, TaskThread>>();
        

        private const float stateListWeight = 1f;
        private const float sleepListWeight = 1f;
        private const float cycleListWeight = 1f;
        private const float queueCountWeight = 1f;

        private void Setup(int threadCount)
        {
            lock (taskThreads)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    TaskThread thread = new TaskThread(this);
                    thread.Start();
                    taskThreads.Add(thread);
                }
            }

            managerThread = new Thread(RunManager);
            managerThread.Start();
        }

        private List<TaskThread> taskThreads = new List<TaskThread>();

        private SortedList<float, TaskThread> optimalTaskThreadList = new SortedList<float, TaskThread>(Comparer<float>.Create((x, y) =>
        {
            int value = Math.Sign(x - y);
            return value == 0 ? 1 : value;
        }));

        private SortedList<DateTime, Task> timeline = new SortedList<DateTime, Task>();

        private Thread managerThread;

        private volatile bool bIsSleeping;

        private volatile bool bPauseUpdating;

        private volatile bool bShouldShutdown;

        private volatile bool bIsShutdown;
    }

    /// <summary>
    /// A thread that can run multiple tasks
    /// </summary>
    public class TaskThread
    {
        public enum ThreadState
        {
            Waiting,
            Processing,
            Alive,
            ShuttingDown,
            Dead,
            Unknown
        }

        /// <summary>
        /// The current state of the task thread
        /// </summary>
        public ThreadState State
        {
            get { return state; }
        }

        /// <summary>
        /// How many tasks are currently queued
        /// </summary>
        public int TasksQueued
        {
            get
            {
                lock (taskQueue)
                    return taskQueue.Count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return TasksQueued == 0;
            }
        }

        /// <summary>
        /// The average time that it has taken to complete the last few tasks
        /// </summary>
        public float AverageCycleTime
        {
            get
            {
                lock(cycleTimeStats)
                {
                    int length = cycleTimeStats.Length;
                    float sum = 0;
                    for (int i = 0; i < length; i++)
                    {
                        sum += cycleTimeStats[i];
                    }

                    return sum / (float)length;
                }
            }
        }

        /// <summary>
        /// The time that we last went from waiting to alive state
        /// </summary>
        public DateTime LastSleepTime
        {
            get { return lastSleepTime; }
        }

        /// <summary>
        /// The date time that the last task started processing
        /// </summary>
        public DateTime ProcessStartTime
        {
            get { return processStartTime; }
        }

        /// <summary>
        /// Gets if this task thread is in safe mode or not.
        /// A thread is in safe mode whenever it is safe to modify tasks from outside of the task thread
        /// </summary>
        public bool IsInSafeMode
        {
            get { return state == ThreadState.Dead || (bEnterSafeMode && state == ThreadState.Alive); }
        }

        public TaskThread(TaskScheduler scheduler)
        {
            this.scheduler = scheduler;
            this.systemThread = new Thread(RunThread);

            threadMap.Add(systemThread, this);
        }

        public void Start()
        {
            if (state != ThreadState.Dead)
                return;

            state = ThreadState.Alive;
            systemThread.Start();
        }

        public void Wake()
        {
            if (state == ThreadState.Waiting)
                systemThread.Interrupt();
        }

        public void ActivateSafeMode()
        {
            bEnterSafeMode = true;
        }

        public void WakeFromSafeMode()
        {
            if (bEnterSafeMode)
                Wake();
        }

        public bool AddTask(Task task)
        {
            if (bShouldShutdown)
                return false;

            lock(taskQueue)
            {
                taskQueue.Enqueue(task);
            }

            Wake();

            return true;
        }

        public void Shutdown()
        {
            bShouldShutdown = true;
            Wake();
        }

        private void RunThread()
        {
            state = ThreadState.Alive;

            Debug.Log.WriteLine("Thread started.");

            /*While the thread is not being shutdown check to see if we can sleep*/
            while (!bShouldShutdown)
            {
                TrySleep();
                ConsumeTask();
            }

            /*It is now safe to assume that the task queue will never be used by another thread (except for reading) and thus it is safe to check its count without locks*/
            while(taskQueue.Count > 0)
            {
                ConsumeTask();
            }

            state = ThreadState.Dead;//there is no more processing and the thread is considered dead
        }

        private void TrySleep()
        {
            if (!bEnterSafeMode)//if we are trying to enter safe mode then do not continue processing (even if tasks are still available)
            {
                lock (taskQueue)
                {
                    if (taskQueue.Count != 0)
                        return;
                }
            }

            try
            {
                state = ThreadState.Waiting;
                Thread.Sleep(Timeout.InfiniteTimeSpan);
            }
            catch(ThreadInterruptedException)
            {
                state = ThreadState.Alive;
                lastSleepTime = DateTime.Now;
                bEnterSafeMode = false;
            }
        }

        private void ConsumeTask()
        {
            state = ThreadState.Processing;

            Task task;

            lock(taskQueue)
            {
                if (taskQueue.Count != 0)
                    task = taskQueue.Dequeue();
                else
                    task = null;
            }

            processStartTime = DateTime.Now;

            if (task != null)
            {
                int delayTime = task.Run();
            }

            DateTime endTime = DateTime.Now;

            TimeSpan cycleTime = endTime - processStartTime;
            AddCycleTime((float)cycleTime.TotalMilliseconds);

            state = ThreadState.Alive;
        }

        private void AddCycleTime(float cycleTime)
        {
            lock (cycleTimeStats)
            {

                int length = cycleTimeStats.Length;
                for (int i = 1; i < length; i++)
                {
                    cycleTimeStats[i] = cycleTimeStats[i - 1];
                }

                cycleTimeStats[0] = cycleTime;

            }
        }

        private Thread systemThread;//the system level thread

        private Queue<Task> taskQueue = new Queue<Task>();

        private TaskScheduler scheduler;

        private DateTime lastSleepTime;
        private DateTime processStartTime;
        private float[] cycleTimeStats = new float[100];

        private volatile ThreadState state = ThreadState.Dead;

        private volatile bool bShouldShutdown;

        private volatile bool bEnterSafeMode;

        private static Dictionary<Thread, TaskThread> threadMap = new Dictionary<Thread, TaskThread>();
    }
}
