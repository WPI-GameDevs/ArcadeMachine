using PorygonOS.Core.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.Processes
{
    class FrontendProcess: BaseProcess
    {

        public static string FrontendExecutableName
        {
            get { return "Frontend.exe"; }
        }

        public static string FrontendPath
        {
            get { return Program.GlobalConfig.GetPath("System", "Frontend").OriginalString; }
        }

        /// <summary>
        /// Constructor - Call the parent and set the pathing correctly.
        /// </summary>
        public FrontendProcess() : 
            base(FrontendExecutableName,
            Path.Combine(FrontendPath, FrontendExecutableName),
            "",
            ProcessSecurityAccess.High)
        {
        }

        /// <summary>
        /// Whenever a new game is installed, call this function to pass of an RPC call to Frontend and let it know there is a new game.
        /// </summary>
        /// <param name="installationPath">The path to the game that was just installed so the frontend knows where to look.</param>
        public void GameInstalled(string installationPath)
        {
            //Set the command string so the frontend know what RPC call this is
            string cmd = "Install";

            //Bundle the arguments into an array to pass to the RPC call
            string[] args = new string[] { installationPath };

            //Pass off the information from this installation to the BaseProcess to pass to the frontend.
            SendRPC(cmd, args);
        }
    }
}
