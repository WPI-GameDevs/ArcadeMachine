using PorygonOS.Core.Commands;
using PorygonOS.Core.Config;
using PorygonOS.Core.Debug;
using PorygonOS.Core.RPC;
using PorygonOS.Core.Tasks;
using PorygonOS.Core.HelperClasses;
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

            CommandExecuter commandExecuter = new CommandExecuter();
            commandExecuter.Execute(args);

            Thread keyboardInterceptorThread = new Thread(SetupKeyboardInterceptor);
            keyboardInterceptorThread.Start();

            scheduler = new Core.Tasks.TaskScheduler();

            Thread readInThread = new Thread(ReadIn);
            readInThread.Start();

            while(bRunning)
            {
                Thread.Sleep(10000);
            }

            readInSafeAccess.WaitOne(Timeout.InfiniteTimeSpan, true);
            readInThread.Abort();
            readInSafeAccess.ReleaseMutex();

            scheduler.Shutdown();
            scheduler.WaitForShutdownComplete();

            globalConfig.Save();

            systemLock.ReleaseMutex();
        }

        static void ReadIn()
        {
            CommandExecuter commandExecuter = new CommandExecuter();

            while (true)
            {
                string commandString = Console.ReadLine();

                readInSafeAccess.WaitOne(Timeout.InfiniteTimeSpan, true);
                string[] commands = commandExecuter.BreakCommandString(commandString);
                commandExecuter.Execute(commands);
                readInSafeAccess.ReleaseMutex();
            }
        }

        /// <summary>
        /// Set up the keyboard interceptor in another thread so it can get the keys that are pressed.
        /// </summary>
        [STAThread]
        static void SetupKeyboardInterceptor()
        {
            KeyboardInterceptor keyboardInterceptor = new KeyboardInterceptor();
            keyboardInterceptor.Start();
        }

        private static bool bRunning = true;

        private static ConfigFile globalConfig;
        private static ConfigFile sharedConfig;

        private static PorygonOS.Core.Tasks.TaskScheduler scheduler;

        private static Mutex readInSafeAccess = new Mutex();
    }
}
