using PorygonOS.Core.Commands;
using PorygonOS.Core.Config;
using PorygonOS.Core.Debug;
using PorygonOS.Core.RPC;
using PorygonOS.Core.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PorygonOS
{
    class Program
    {
        public static bool IsRunning
        {
            get { return bRunning; }
        }

        public static ConfigFile GlobalConfig
        {
            get { return globalConfig; }
        }

        public static void Shutdown()
        {
            bRunning = false;
        }

        static void Main(string[] args)
        {
            globalConfig = ConfigFile.Create("config.ini");
            sharedConfig = ConfigFile.Create("shared.ini");

            string systemLockKey = globalConfig.GetString("System", "MutexKey");
            Mutex systemLock = new Mutex(false, systemLockKey);

            if (!systemLock.WaitOne(TimeSpan.Zero, true))
                return;

            Log.ShowDate = true;//show the date of when logs happen
            Log.ShowThreadID = true;//show the thread id that this message is from

            RPCManager.Create(".");

            TaskThread.StartupDefault();//startup the task threads

            CommandExecuter commandExecuter = new CommandExecuter();
            commandExecuter.Execute(args);

            while(bRunning)
            {
                string commandString = Console.ReadLine();
                string[] commands = commandExecuter.BreakCommandString(commandString);
                commandExecuter.Execute(commands);
            }

            TaskThread.ShutdownAll();

            globalConfig.Save();
        }

        private static bool bRunning = true;

        private static ConfigFile globalConfig;
        private static ConfigFile sharedConfig;
    }
}
