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
    struct TaskSchedule
    {
        public Task ScheduledTask
        {
            get { return task; }
            set { task = value; }
        }

        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }

        public TaskSchedule(Task task, DateTime timeStamp)
        {
            this.task = task;
            this.timeStamp = timeStamp;
        }

        public void Serialize(BinaryWriter writer)
        {
            Type taskType = task.GetType();
            bool isBootable = taskType.IsDefined(typeof(NoBoot), false);

            if(!isBootable)
                return;

            writer.Write(taskType.FullName);
            writer.Write(timeStamp.ToLongDateString());

            bool serializeable = !taskType.IsDefined(typeof(NonSerializedAttribute), true);
            if (!serializeable)
                return;

            task.Serialize(writer);
        }

        private Task task;
        private DateTime timeStamp;
    }

    /// <summary>
    /// A thread that can run multiple tasks
    /// </summary>
    public class TaskThread
    {
        enum ThreadState
        {
            Waiting,
            Processing,
            Alive,
            ShuttingDown,
            Dead,
            Unknown
        }

        public static void StartupDefault()
        {
            int processorCount = Environment.ProcessorCount;//get the number of processors

            for(int i = 0; i < processorCount; i++)
            {
                TaskThread thread = new TaskThread(ThreadPriority.Normal);
                thread.Start();
            }

            managerThread = new Thread(ManageThreads);
            managerThread.Start();
        }

        private static void ManageThreads()
        {
            while(!isInShutdown)
            {
                int threadCount;
                lock (taskThreads)
                    threadCount = taskThreads.Count;

                DateTime now = DateTime.Now;

                for (int i = 0; i < threadCount; i++)
                {
                    TaskThread mainThread;
                    lock (taskThreads)
                        mainThread = taskThreads[i];

                    if (mainThread.state != ThreadState.Processing)
                        continue;

                    TimeSpan noResponseTime = now - mainThread.lastRefreshTime;
                    if (noResponseTime.TotalSeconds < 10f)
                        continue;

                    mainThread.Ghost();
                }

                Thread.Sleep(new TimeSpan(0, 0, 0, 10));//sleep 10 seconds
            }
        }

        public static void SaveBootFile(string bootFile = "default.boot")
        {
            FileStream stream = new FileStream(bootFile, FileMode.OpenOrCreate, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(stream);

            foreach(TaskThread thread in taskThreads)
            {
                thread.SaveForBoot(writer);
            }

            writer.Close();
        }

        public static bool ReadBootFile(string bootFile = "default.boot")
        {
            if (!File.Exists(bootFile))
                return false;

            FileStream stream = new FileStream(bootFile, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            while (reader.ReadBoolean())
            {
                string taskName = reader.ReadString();
                Type taskType = null;
                try
                {
                    taskType = Type.ReflectionOnlyGetType(taskName, true, false);
                }
                catch (TypeLoadException)
                {
                    Log.WriteLine("Could not load boot file {0}.", bootFile);
                    reader.Close();
                    return false;
                }

                DateTime timeStamp = DateTime.Parse(reader.ReadString());

                Task task;
                bool serialized = reader.ReadBoolean();
                if(serialized)
                {
                    task = FormatterServices.GetUninitializedObject(taskType) as Task;
                    if(task == null)
                    {
                        Log.WriteLine("Could not load task {0}.", taskName);
                        reader.Close();
                        return false;
                    }

                    task.Deserialize(reader);
                }
                else
                {
                    task = Activator.CreateInstance(taskType) as Task;
                    if (task == null)
                    {
                        Log.WriteLine("Could not load task {0}.", taskName);
                        reader.Close();
                        return false;
                    }
                }

                Schedule(task, timeStamp);
            }

            return true;
        }

        /// <summary>
        /// Gets the task thread for the current thread
        /// </summary>
        /// <returns>The task thread for the current thread</returns>
        public static TaskThread GetCurrent()
        {
            TaskThread thread = null;
            threadTable.TryGetValue(Thread.CurrentThread, out thread);
            return thread;
        }

        public double ProcessingLoad
        {
            get
            {
                if (taskList.Count == 0)
                    return 0;

                double totalTime = waitTime + processingTime;
                return processingTime / waitTime;//get the percentage of time spent processing
            }
        }

        public uint ThreadID
        {
            get { return threadID; }
        }

        public TaskThread(System.Threading.ThreadPriority priority)
        {
            thread = new Thread(RunThread);
            thread.Priority = priority;
            threadID = threadIdGeneratorCount;
            threadIdGeneratorCount++;
        }

        public void Start()
        {
            thread.Start();

            taskThreads.Add(this);
            threadTable.Add(thread, this);

            Log.WriteLine("Task thread started.");
        }

        public void Shutdown()
        {
            state = ThreadState.ShuttingDown;
            Wake();
        }

        public static void ShutdownAll()
        {
            isInShutdown = true;

            foreach(TaskThread thread in taskThreads)
            {
                thread.Shutdown();
            }

            while (taskThreads.Count > 0)
                Thread.Sleep(1000);
        }

        /// <summary>
        /// Wakes the thread if it is currently sleeping
        /// </summary>
        public void Wake()
        {
            lock(monitor)
            {
                Monitor.Pulse(monitor);
            }
        }

        public static void Schedule(Task task, int milliseconds)
        {
            if (milliseconds < 0)
                return;

            TaskThread best = GetBestTaskThread();

            if (best != null)
                best.ScheduleTask(task, milliseconds);
            else
                Console.Error.WriteLine("Error: No task threads running");
        }

        public static void Schedule(Task task, DateTime timeStamp)
        {
            DateTime now = DateTime.Now;
            timeStamp = now < timeStamp ? timeStamp : now;

            TaskThread best = GetBestTaskThread();

            if (best != null)
                best.ScheduleTask(task, timeStamp);
            else
                Console.Error.WriteLine("Error: No task threads running");
        }

        public static TaskThread GetBestTaskThread()
        {
            TaskThread best = null;
            double bestRatio = double.MaxValue;
            int taskThreadCount = taskThreads.Count;

            for (int i = 0; i < taskThreadCount; i++)
            {
                TaskThread tt = taskThreads[i];

                double load = tt.ProcessingLoad;
                if (load < bestRatio)
                {
                    bestRatio = load;
                    best = tt;
                }
            }
            return best;
        }

        private void ScheduleTask(Task task, int milliseconds)
        {
            if (milliseconds < 0)
                throw new ArgumentException("Time cannot be less than 0.", "milliseconds");

            TimeSpan offset = new TimeSpan(0, 0, 0, 0, milliseconds);

            ScheduleTask(task, DateTime.Now + offset);
        }

        private void ScheduleTask(Task task, DateTime timestamp)
        {
            TaskSchedule newSchedule = new TaskSchedule(task, timestamp);
            bool added = false;
            lock (taskList)
            {
                for (LinkedListNode<TaskSchedule> node = taskList.First; node != null; node = node.Next)
                {
                    TaskSchedule t = node.Value;
                    if (newSchedule.TimeStamp <= t.TimeStamp)
                    {
                        taskList.AddBefore(node, newSchedule);
                        added = true;
                        break;
                    }
                }
            }

            if (!added)
                taskList.AddLast(newSchedule);

            Wake();
        }

        private void RunThread()
        {
            state = ThreadState.Alive;

            while(state != ThreadState.ShuttingDown)
            {
                RunCycle();
            }

            BeginShutdown();

            state = ThreadState.Dead;
        }

        private void RunCycle()
        {
            DateTime startProcessTime = DateTime.Now;//record the start time of processing

            bool didProcess = RunTasks();//run the tasks, see if we actually processed any

            if (isGhosting)//if we did ghost then kill the thread
                return;

            DateTime endProcessTime = DateTime.Now;//record the end time of processing

            if (didProcess)
                processingTime = (processingTime + (endProcessTime - startProcessTime).TotalSeconds) / 2.0;

            TimeSpan waitTime;
            if (taskList.Count > 0)
            {
                TaskSchedule next = taskList.First.Value;
                DateTime timeStamp = next.TimeStamp;
                waitTime = DateTime.Now - timeStamp;
            }
            else
                waitTime = (state == ThreadState.ShuttingDown) ?  new TimeSpan(0, 0, 10) : new TimeSpan(1, 0, 0);

            DateTime estimatedEndWaitTime = DateTime.Now + waitTime;//record the estimated end time for waiting

            this.waitTime += (estimatedEndWaitTime - endProcessTime).TotalSeconds;
            this.waitTime = this.waitTime / 2.0;

            lock (monitor)
            {
                Monitor.Wait(monitor, waitTime);
            }

            DateTime endWaitTime = DateTime.Now;//record the actual end wait time
        }

        private void BeginShutdown()
        {
            foreach(TaskSchedule t in taskList)
            {
                t.ScheduledTask.Shutdown();
            }

            while (taskList.Count > 0)
                RunCycle();

            lock(taskThreads)
                taskThreads.Remove(this);
            lock (threadTable)
                threadTable.Remove(this.thread);

            Log.WriteLine("Task thread shutdown.");
        }

        private bool RunTasks()
        {
            bool didProcess = false;
            while (taskList.Count > 0)
            {
                lock (ghostedThreads)//check if this thread has begun ghosting, if so then quit
                    if (ghostedThreads.Contains(Thread.CurrentThread))
                        isGhosting = true;

                if (isGhosting)
                    break;

                DateTime now = DateTime.Now;//if tasks are slow enough, then a significant amount of time could pass
                lastRefreshTime = now;//mark a refresh so we don't ghost

                TaskSchedule task = taskList.First.Value;
                DateTime timeStamp = task.TimeStamp;

                if (now >= timeStamp)
                {
                    didProcess = true;
                    lock(taskList)
                        taskList.RemoveFirst();
                    int rescheduleTime = task.ScheduledTask.Run();
                    if (rescheduleTime > 0)
                        ScheduleTask(task.ScheduledTask, rescheduleTime);
                }
                else//if we find a task that is still in the future then break
                {
                    didProcess = true;
                    break;
                }
            }

            lock (ghostedThreads)//check if this thread has begun ghosting, if so then quit
                if (ghostedThreads.Contains(Thread.CurrentThread))
                    isGhosting = true;

            return didProcess;
        }

        private void SaveForBoot(BinaryWriter writer)
        {
            foreach(TaskSchedule task in taskList)
            {
                writer.Write(true);
                task.Serialize(writer);
            }

            writer.Write(false);
        }

        private void Ghost()
        {
            lock (ghostedThreads)
                ghostedThreads.Add(thread);

            thread = new Thread(RunCycle);
            thread.Start();
        }

        private Thread thread;

        private ThreadState state;

        private LinkedList<TaskSchedule> taskList = new LinkedList<TaskSchedule>();

        private object monitor = new object();//

        private double waitTime;
        private double processingTime;

        private DateTime lastRefreshTime;

        private uint threadID;

        #region THREAD_VARIABLES

        /*Here are the variables that are specific to this thread and can only be accessed from within the thread*/
        [ThreadStatic]
        bool isGhosting;

        #endregion

        private static Thread managerThread;
        private static List<TaskThread> taskThreads = new List<TaskThread>();
        private static Dictionary<Thread, TaskThread> threadTable = new Dictionary<Thread, TaskThread>();
        private static uint threadIdGeneratorCount = 0;
        private static bool isInShutdown = false;

        private static List<Thread> ghostedThreads = new List<Thread>();//threads that have been set to ghost
    }
}
