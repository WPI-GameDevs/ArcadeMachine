using PorygonOS.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.Processes
{
    [NoBoot]
    public class ProcessWatcher : PorygonOS.Core.Tasks.Task
    {
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override void Deserialize(System.IO.BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        protected override int OnRun()
        {
            foreach(BaseProcess process in BaseProcess.IterateProcesses())
            {
                process.Handle();
            }

            return 10000;
        }
    }
}
