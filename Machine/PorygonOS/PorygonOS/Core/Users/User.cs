using PorygonOS.Core.Config;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
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

            gamePath = config.GetPath("SystemInfo", "GameLocation").OriginalString;
            rootPath = config.GetPath("SystemInfo", "RootLocation").OriginalString;

            DirectoryInfo gameDirectoryInfo = Directory.CreateDirectory(gamePath);
            gameDirectoryInfo.Create();

            string defaultUserPath = Program.GlobalConfig.GetPath("User", "DefaultUser").OriginalString;
            DirectoryInfo defaultUserPathInfo = new DirectoryInfo(defaultUserPath);

            DirectoryInfo rootDirectoryInfo = defaultUserPathInfo.Copy(rootPath);

            FileSystemAccessRule accessRule = new FileSystemAccessRule(userName, FileSystemRights.Modify, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
            FileSystemAccessRule denyRule = new FileSystemAccessRule(userName, FileSystemRights.TakeOwnership | FileSystemRights.ChangePermissions, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);

            DirectorySecurity rootSecurity = rootDirectoryInfo.GetAccessControl();
            rootSecurity.AddAccessRule(accessRule);
            rootSecurity.AddAccessRule(denyRule);
            rootDirectoryInfo.SetAccessControl(rootSecurity);

            DirectorySecurity gameSecurity = gameDirectoryInfo.GetAccessControl();
            gameSecurity.AddAccessRule(accessRule);
            gameSecurity.AddAccessRule(denyRule);
            gameDirectoryInfo.SetAccessControl(gameSecurity);



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

        private string gamePath;

        private string rootPath;

        private ConfigFile config;

        private bool isValid;

        private UserPrincipal systemUser;

        private const PrincipalContext localContext = new PrincipalContext(ContextType.Machine);

        private static SortedDictionary<string, User> userTable = new SortedDictionary<string, User>();
    }
}
