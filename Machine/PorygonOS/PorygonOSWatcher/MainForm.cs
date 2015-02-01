using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PorygonOS.Core.Config;

namespace PorygonOSWatcher
{
    public partial class MainForm : Form
    {
        delegate void RefreshLog(string newText, Color color);

        public MainForm()
        {
            OnLogRefreshed += UpdateLog;
            manager = OSManager.TryCreateInstance(Program.SharedConfig.GetPath("System", "OSExeLocation").OriginalString, this);
            InitializeComponent();
        }

        public void AddLog(string line, bool isIn, Color color)
        {
            StringBuilder strBuilder = new StringBuilder(line.Length);

            string prefix = isIn ? ">>" : "<<";
            strBuilder.Append(prefix);

            strBuilder.Append(line);
            strBuilder.AppendLine();

            string result = strBuilder.ToString();

            Invoke(OnLogRefreshed, result, color);
        }

        public void ClearLog()
        {
            logOutBox.Text = "";
        }

        private void UpdateLog(string message, Color color)
        {
            logOutBox.SelectionStart = logOutBox.TextLength;
            logOutBox.SelectionLength = 0;
            logOutBox.SelectionColor = color;

            logOutBox.AppendText(message);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            
        }

        private void sendLogButton_Click(object sender, EventArgs e)
        {
            string outText = this.logInText.Text;
            this.logInText.Text = "";

            if (string.IsNullOrWhiteSpace(outText))
                return;

            Color logColor;
            if (outText.StartsWith("##&RPC"))
                logColor = Color.Purple;
            else
                logColor = Color.BlueViolet;

            AddLog(outText, true, logColor);
        }

        private void launchButton_Click(object sender, EventArgs e)
        {
            manager.Start();
        }

        private void shutdownButton_Click(object sender, EventArgs e)
        {
            manager.Close();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if DEBUG
            manager.Close();
#endif
        }

        private OSManager manager;

        private RefreshLog OnLogRefreshed;
    }
}
