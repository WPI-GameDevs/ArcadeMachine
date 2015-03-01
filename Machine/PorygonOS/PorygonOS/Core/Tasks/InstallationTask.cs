using PorygonOS.Core.Debug;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PorygonOS.Core.Tasks
{
    /// <summary>
    /// At a scheduled time, the installation task will search a directory for zip files,
    /// unzip them and place the files in their correct directory.
    /// </summary>
    class InstallationTask : Task
    {
        private string installationDirectoryName = "../Install/";
        private string gameBankDirectory = "../GameBank/";
        private string gameInfoSection = "GameInfo";
        private string teamNameVariable = "team";
        private string gameNameVariable = "name";
        private static Timer aTimer; 

        public override void Serialize(BinaryWriter writer)
        {

        }

        public override void Deserialize(BinaryReader reader)
        {

        }

        /// <summary>
        /// Unzip and install all games in a directory.
        /// </summary>
        /// <returns></returns>
        protected override int OnRun()
        {
            return 0;
            string currentDir = System.IO.Directory.GetCurrentDirectory();

            //Get all zip files from the installation directory
            string[] filePaths = Directory.GetFiles(installationDirectoryName, "*.zip");

            foreach(string zipFile in filePaths)
            {
                //Choose what folder to extract to.
                string folderName = Path.GetFileNameWithoutExtension(zipFile);
                string extractedPath = installationDirectoryName + folderName;

                //Unzip the files into folders
                DeleteDirectoryIfItExists(extractedPath);
                ZipFile.ExtractToDirectory(zipFile, installationDirectoryName);

                //Find the configuration.ini file in the new directoy
                string[] iniFiles = Directory.GetFiles(extractedPath, "*.ini");

                //Parse the team name and game name out of the config
                string teamName = GetParameterFromConfig(teamNameVariable, iniFiles[0]);
                string gameName = GetParameterFromConfig(gameNameVariable, iniFiles[0]);

                //Find or create a directory with the specified team name in the game bank
                string finalDirectoryPath = gameBankDirectory + teamName + "/";
                string finalDirectory = finalDirectoryPath + gameName;
                DeleteDirectoryIfItExists(finalDirectory);//Delete the game directory if this is an update.
                Directory.CreateDirectory(finalDirectoryPath);//Make the directory to hold the game.
                Directory.Move(extractedPath, finalDirectory);//Move the extracted files to the game folder.

                //Delete the zip file.
                File.Delete(zipFile);
            }

            //set up timer if not set up yet
            if (aTimer == null)
            {
                // Create a timer with a 24 hour interval.
                aTimer = new System.Timers.Timer(86400000);
                //aTimer = new System.Timers.Timer(2000);
                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += OnTimedEvent;
                aTimer.Enabled = true;
            }
            //Return success when all of the games are done.
            return 0;
        }

        /// <summary>
        /// Parse the team name, game name, etc, out of a given config file using the IniParser.
        /// </summary>
        /// <param name="configFilePath"></param>
        /// <returns></returns>
        private string GetParameterFromConfig(string param, string configFilePath)
        {
            IniParser.FileIniDataParser parser = new IniParser.FileIniDataParser();
            IniParser.Model.IniData configData = parser.ReadFile(configFilePath);
            IniParser.Model.KeyDataCollection gameInfo = configData[gameInfoSection];
            return gameInfo.GetKeyData(param).Value;
        }

        /// <summary>
        /// Delete a directory, recursively.
        /// </summary>
        /// <param name="path"></param>
        private void DeleteDirectoryIfItExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);//Delete the extracted files if they already exist.
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Running innstallation at {0}", e.SignalTime);
            OnRun();

        }
    }
}
