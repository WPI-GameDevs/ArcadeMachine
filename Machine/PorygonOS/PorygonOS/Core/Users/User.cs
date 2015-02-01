using PorygonOS.Core.Config;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core.Users
{
    public class User
    {
        public static string UserBankDirectory
        {
            get { return Program.GlobalConfig.GetPath("Users", "UserBank").OriginalString; }
        }

        public void User(string userName)
        {
            this.userName = userName;
            Load();
        }

        public void Load()
        {
            userPath = Path.Combine(UserBankDirectory, userName);//get the users directory
            if (!Directory.Exists(userPath))
                return;

            string configPath = Path.Combine(userPath, "UserConfig.ini");
            config = ConfigFile.Create(configPath);

            if (config == null)
                return;
        }

        /// <summary>
        /// Installs the user on the machine
        /// <returns>True if the install worked, false otherwise</returns>
        /// </summary>
        public bool Install()
        {
            if (userTable.ContainsKey(userName))
                return false;

            Load();//attempt a load before running the install procedure
            if (isValid)//if the load was valid do not install
                return true;//the install was valid, even though it didn't run

            if(!Directory.Exists(userPath))
                return false;

            if(config == null)
            {
                string configPath = Path.Combine(userPath, "UserConfig.ini");
                config = ConfigFile.Create(configPath);
                if (config == null)
                    return false;
            }

            try
            {
                systemUser = UserPrincipal.FindByIdentity(localContext, IdentityType.Name, userName);
            }
            catch
            {
                return false;
            }

            if (systemUser == null)
                systemUser = new UserPrincipal(localContext);

            systemUser.SetPassword(key);
            systemUser.Name = userName;
            systemUser.DisplayName = displayName;
            systemUser.PasswordNeverExpires = true;
            systemUser.HomeDirectory = userPath;
            
            try
            {
                systemUser.Save();
            }
            catch
            {
                return false;
            }

            userTable.Add(userName, this);
            UpdateInstallFile();

            return true;
        }

        public static void UpdateInstallFile()
        {
            string installFile = Program.GlobalConfig.GetPath("Installs", "InstalledUserList").OriginalString;

            FileStream stream = new FileStream(installFile, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter writer = new StreamWriter(stream);

            foreach(KeyValuePair<string, User> userData in userTable)
            {
                writer.WriteLine(userData.Key);
            }

            writer.Close();
        }

        private string userName;

        private string displayName;

        private string key;

        private string userPath;

        private ConfigFile config;

        private bool isValid;

        private UserPrincipal systemUser;

        private const PrincipalContext localContext = new PrincipalContext(ContextType.Machine);

        private static SortedDictionary<string, User> userTable = new SortedDictionary<string, User>();
    }
}
