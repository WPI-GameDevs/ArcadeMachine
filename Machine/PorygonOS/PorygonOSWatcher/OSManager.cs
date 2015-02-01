using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PorygonOSWatcher
{
    public class OSManager
    {
        public static OSManager Instance
        {
            get { return instance; }
        }

        public static OSManager TryCreateInstance(string osLocation, MainForm form)
        {
            if (instance != null)
                return instance;

            new OSManager(osLocation, form);

            return instance;
        }

        private OSManager(string osLocation, MainForm form)
        {
            instance = this;

            this.form = form;

            process = new Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = osLocation;
            startInfo.WorkingDirectory = Path.GetDirectoryName(osLocation);
#if !DEBUG
            startInfo.CreateNoWindow = true;
#endif
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;

            process.EnableRaisingEvents = true;
            process.Exited += OnClosed;
            process.OutputDataReceived += OnDataRecieved;
        }

        public void Start()
        {
            if (!isRunning)
            {
                isRunning = process.Start();

                if (isRunning)
                {
                    form.AddLog("OS Started...", true, Color.Black);
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }
        }

        public void Close()
        {
            if (isRunning)
            {
                isRunning = false;
                form.AddLog("OS Shutting down...", true, Color.Black);
                process.Kill();
            }
        }

        private void OnClosed(object sender, EventArgs args)
        {
            if(isRunning)
            {

            }
        }

        private void OnDataRecieved(object sender, DataReceivedEventArgs args)
        {
            string data = args.Data;
            if (string.IsNullOrEmpty(data))
                return;

            Color logColor = data.StartsWith("##&RPC") ? Color.Purple : Color.Black;

            form.AddLog(data, false, logColor);
        }

        private void OnErrorRecieved(object sender, DataReceivedEventArgs args)
        {
            string data = args.Data;

            form.AddLog(data, false, Color.Red);
        }

        private Process process;

        private static OSManager instance;

        private MainForm form;

        private bool isRunning;
    }
}
