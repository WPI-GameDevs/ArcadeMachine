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
    public class GameProcess : BaseProcess
    {
        public static string GameBankPath
        {
            get { return Program.GlobalConfig.GetPath("System", "GameBank").OriginalString; }
        }

        public static string GameUser
        {
            get { return Program.GlobalConfig.GetString("User", "GameUser"); }
        }

        public static string GamePassword
        {
            get { return Program.GlobalConfig.GetString("User", "GamePassword"); }
        }

        public GameProcess(string name) : 
            base(name, 
            Path.Combine(GameBankPath, name),
            "",
            ProcessSecurityAccess.Low)
        {
            config = ConfigFile.Create(Path.Combine(GameBankPath, "gconfig.ini"));
        }

        protected override ProcessStartInfo GetStartInfo()
        {
            ProcessStartInfo startInfo = base.GetStartInfo();
            startInfo.Arguments = config.GetString("StartInfo", "args");
            startInfo.UserName = GameUser;

            string password = GamePassword;
            SecureString securePassword = new SecureString();

            int charLength = password.Length;
            for(int i = 0; i < charLength; i++)
            {
                char c = password[i];
                securePassword.AppendChar(c);
            }

            startInfo.Password = password;

            startInfo.LoadUserProfile = true;

            return startInfo;
        }

        protected override void OnPostStart()
        {
            Process systemProcess = SystemProcess;

            while (systemProcess.MainWindowHandle == IntPtr.Zero) { }

            MoveWindow();
        }

        private void MoveWindow()
        {
            
        }

        private ConfigFile config;
    }
}
