using PorygonOS.Core.Debug;
using PorygonOS.Core.RPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PorygonOS.Core.Processes
{
    /// <summary>
    /// The security level that a process has been granted
    /// </summary>
    public enum ProcessSecurityAccess
    {
        None,
        Low,
        Medium,
        High
    }

    public class BaseProcess
    {
        /// <summary>
        /// Get a process by name.
        /// </summary>
        /// <param name="name">The name of the process</param>
        /// <returns>The base process, or null if no process of this name exists</returns>
        public static BaseProcess GetProcess(string name)
        {
            BaseProcess process = null;
            processTable.TryGetValue(name, out process);
            return process;
        }

        public static IEnumerator<BaseProcess> IterateProcesses()
        {
            return processTable.Values.GetEnumerator();
        }

        /// <summary>
        /// The system view of the process
        /// </summary>
        public Process SystemProcess
        {
            get { return systemProcess; }
        }

        public ProcessSecurityAccess SecurityLevel
        {
            get { return securityLevel; }
        }

        public BaseProcess(string name, string file, string args, ProcessSecurityAccess accessLevel, ProcessPriorityClass priority = ProcessPriorityClass.Normal)
        {
            this.name = name;
            this.file = file;
            this.args = args;
            this.priority = priority;
            this.securityLevel = accessLevel;

            systemProcess = new Process();
        }

        public void Start()
        {
            if (!OnPreStart())
                return;

            ProcessStartInfo startInfo = GetStartInfo();

            systemProcess.StartInfo = startInfo;
            systemProcess.PriorityClass = priority;

            systemProcess.EnableRaisingEvents = true;
            systemProcess.Exited += systemProcess_Exited;

            systemProcess.OutputDataReceived += OnDataReceived;

            string logFile = Path.Combine(startInfo.WorkingDirectory, "log.txt");
            FileStream log = new FileStream(logFile, FileMode.OpenOrCreate, FileAccess.Write);
            logWriter = new StreamWriter(log);

            if(!systemProcess.Start())
            {
                Log.WriteLine("Process {0} failed to start.", systemProcess.ProcessName);
                logWriter.WriteLine("[SYSTEM MESSAGE]: THE PROCESS FAILED TO START.");
                logWriter.Close();
                logWriter = null;
                OnStartFailed();
            }
            else
            {
                isAlive = true;
                systemProcess.BeginOutputReadLine();
                lastRespondeTime = DateTime.Now;
                OnPostStart();
            }
        }

        public void Close()
        {
            systemProcess.CloseMainWindow();
        }

        public void Kill()
        {
            systemProcess.Kill();
        }

        public void Handle()
        {
            if (!isAlive)
                return;

            if (!Check())
                if (!SafeReset())
                    Kill();
        }

        public void SendData(string data)
        {
            systemProcess.StandardInput.WriteLine(data);
        }

        public void SendRPC(string cmd, params string[] args)
        {
            StringBuilder commandLineBuilder = new StringBuilder("##&RPC[");
            commandLineBuilder.Append(this.systemProcess.ProcessName);
            commandLineBuilder.Append("] ");
            commandLineBuilder.Append(" ");
            commandLineBuilder.Append(cmd);

            foreach(string arg in args)
            {
                commandLineBuilder.Append(arg);
            }

            SendData(commandLineBuilder.ToString());
        }

        protected virtual ProcessStartInfo GetStartInfo()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(file, args);
            startInfo.WorkingDirectory = Path.GetDirectoryName(file);
            startInfo.ErrorDialog = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.Arguments = args;
            startInfo.CreateNoWindow = true;

            return startInfo;   
        }

        protected virtual bool OnPreStart() { return true; }

        protected virtual void OnPostStart() { }

        protected virtual void OnStartFailed() { }

        protected virtual void OnClosed(int exitCode)
        {
            
        }

        protected void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            string data = e.Data;

            if (securityLevel != ProcessSecurityAccess.None)
            {
                RPC.RPCSecurityLevel rpcSecurityLevel;
                switch(securityLevel)
                {
                    case ProcessSecurityAccess.Low:
                        rpcSecurityLevel = RPC.RPCSecurityLevel.Low;
                        break;
                    case ProcessSecurityAccess.Medium:
                        rpcSecurityLevel = RPC.RPCSecurityLevel.Medium;
                        break;
                    case ProcessSecurityAccess.High:
                        rpcSecurityLevel = RPC.RPCSecurityLevel.High;
                        break;
                }

                RPCManager.Instance.ProcessCommand(data, rpcSecurityLevel);
            }

            if(logWriter != null)
                logWriter.WriteLine(data);
        }

        protected virtual bool Check()
        {
            if(systemProcess.Responding)
            {
                lastRespondeTime = DateTime.Now;
                return true;
            }

            TimeSpan frozenTime = DateTime.Now - lastRespondeTime;
            return frozenTime.TotalSeconds < 30;
        }

        protected virtual bool SafeReset()
        {
            Kill();

            Thread.Sleep(50);//give some time for the process to kill

            Start();

            return true;
        }

        private void Closed()
        {
            isAlive = false;

            int exitCode = systemProcess.ExitCode;

            Log.WriteLine("Process {0} exited with code  {1}.", systemProcess.ProcessName, exitCode);

            OnClosed(exitCode);

            logWriter.Close();
            logWriter = null;
        }

        private void systemProcess_Exited(object sender, EventArgs e)
        {
            Closed();
        }

        private string name;

        private Process systemProcess;

        private string file;

        private string args;

        private ProcessPriorityClass priority;

        private ProcessSecurityAccess securityLevel;

        private bool isAlive;

        private StreamWriter logWriter;

        private DateTime lastRespondeTime;

        private static Dictionary<string, BaseProcess> processTable = new Dictionary<string, BaseProcess>();
    }
}
