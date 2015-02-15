using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.Commands
{
    [Command("-exit")]
    public class ExitCommand
    {
        public void Execute()
        {
            Program.Shutdown();
            Debug.Log.WriteLine("Exiting program...");
        }
    }
}
