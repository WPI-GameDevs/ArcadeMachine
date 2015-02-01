using PorygonOS.Core.Debug;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private Task task;
        private DateTime timeStamp;
    }

    /// <summary>
    /// A thread that can run multiple tasks
    /// </summary>
    public class TaskThread
    {
        public static void StartupDefault()
        {
            int processorCount = Environment.ProcessorCount;//get the number of processors

            for(int i = 0; i < processorCount; i++)
            {
                TaskThread thread = new TaskThread(ThreadPriority.Normal);
                thread.Start();
            }
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
            bShutdown = true;
            Wake();
        }

        public static void ShutdownAll()
        {
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
            TaskThread best = null;
            double bestRatio = double.MaxValue;
            int taskThreadCount = taskThreads.Count;

            for(int i = 0; i < taskThreadCount; i++)
            {
                TaskThread tt = taskThreads[i];

                double load = tt.ProcessingLoad;
                if(load < bestRatio)
                {
                    bestRatio = load;
                    best = tt;
                }
            }

            if (best != null)
                best.ScheduleTask(task, milliseconds);
            else
                Console.Error.WriteLine("Error: No task threads running");
        }

        private void ScheduleTask(Task task, int milliseconds)
        {
            if (milliseconds < 0)
                throw new ArgumentException("Time cannot be less than 0.", "milliseconds");

            TimeSpan offset = new TimeSpan(0, 0, 0, 0, milliseconds);
            DateTime timestamp = DateTime.Now + offset;

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
            bAlive = true;

            while(!bShutdown)
            {
                RunCycle();
            }

            BeginShutdown();

            bAlive = false;
        }

        private void RunCycle()
        {
            DateTime startProcessTime = DateTime.Now;//record the start time of processing

            bool didProcess = RunTasks();//run the tasks, see if we actually processed any

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
                waitTime = bShutdown ?  new TimeSpan(0, 0, 10) : new TimeSpan(1, 0, 0);

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
                DateTime now = DateTime.Now;//if tasks are slow enough, then a significant amount of time could pass

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
                    return didProcess;
            }

            return didProcess;
        }

        private Thread thread;

        private bool bShutdown;//when true the thread will shutdown
        private bool bAlive;//if true then this task thread is still running

        private LinkedList<TaskSchedule> taskList = new LinkedList<TaskSchedule>();

        private object monitor = new object();//

        private double waitTime;
        private double processingTime;

        private uint threadID;


        private static List<TaskThread> taskThreads = new List<TaskThread>();
        private static Dictionary<Thread, TaskThread> threadTable = new Dictionary<Thread, TaskThread>();
        private static uint threadIdGeneratorCount = 0;
    }
}
