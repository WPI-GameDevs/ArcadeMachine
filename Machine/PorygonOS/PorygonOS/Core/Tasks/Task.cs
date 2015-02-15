using PorygonOS.Core.Debug;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.Tasks
{
    /// <summary>
    /// A task to be run
    /// </summary>
    public abstract class Task
    {
        /// <summary>
        /// Are we shutting down
        /// </summary>
        protected bool IsShuttingDown
        {
            get { return bShutdown; }
        }

        public Task()
        {

        }

        /// <summary>
        /// Called every time this task should run
        /// </summary>
        /// <returns>The number of milliseconds to wait before running again, or <= 0 to stop</returns>
        public int Run()
        {
            Log.WriteLine("Running {0}.", GetType().Name);
            int waitTime = OnRun();
            Log.WriteLine("Finished {0}.", GetType().Name);
            return waitTime;
        }

        public void Shutdown()
        {
            bShutdown = true;
        }

        public abstract void Serialize(BinaryWriter writer);

        public abstract void Deserialize(BinaryReader reader);

        protected abstract int  OnRun();

        private volatile bool bShutdown;
    }
}
