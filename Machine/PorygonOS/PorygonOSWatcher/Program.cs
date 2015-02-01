using PorygonOS.Core;
using PorygonOS.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PorygonOSWatcher
{
    public static class Program
    {
        public static ConfigFile SharedConfig
        {
            get { return sharedConfig; }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            sharedConfig = ConfigFile.Create("shared.ini");//get the config file
            string mutexGUID = sharedConfig.GetString("System", "MutexKey");

            Mutex mutex = new Mutex(false, mutexGUID);

            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                mutex.ReleaseMutex();
            }
            else
            {
                //inform the running version to show its window
                NativeMethods.PostMessage((IntPtr)NativeMethods.WM_SHOWME, NativeMethods.WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private static ConfigFile sharedConfig;
    }
}
