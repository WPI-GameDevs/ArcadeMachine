using PorygonOS.Core.Debug;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PorygonOS.Core.Tasks
{
    class FrontendLauncherTask : Task
    {
        public override void Serialize(BinaryWriter writer)
        {

        }

        public override void Deserialize(BinaryReader reader)
        {

        }

        /// <summary>
        /// Launch the FrontendProcess
        /// </summary>
        /// <returns></returns>
        protected override int OnRun()
        {
            Processes.FrontendProcess fp = new Processes.FrontendProcess();
            fp.Start();
            //Return 0 so it never loops
            return 0;
        }

        /// <summary>
        /// Run the frontend launcher at this time.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Running Frontend Launcher at {0}", e.SignalTime);
            OnRun();

        }
    }
}
