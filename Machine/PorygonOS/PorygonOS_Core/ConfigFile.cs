using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace PorygonOS.Core.Config
{
    public class ConfigFile
    {
        public static ConfigFile Create(string file)
        {
            if (configTable.ContainsKey(file))
                return configTable[file];

            if (!File.Exists(file))
                return null;

            file = Path.GetFullPath(file);
            ConfigFile configFile = new ConfigFile(file);
            configTable.Add(file, configFile);
            configFile.Load();

            return configFile;
        }

        private ConfigFile(string file)
        {
            this.file = new Uri(file);
            Load();
        }

        protected virtual void Load()
        {
            FileStream stream = new FileStream(file.OriginalString, FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader reader = new StreamReader(stream);
            StreamIniDataParser parser = new StreamIniDataParser();

            data = parser.ReadData(reader);

            reader.Close();
        }

        public virtual void Save()
        {
            FileStream stream = new FileStream(file.OriginalString, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter writer = new StreamWriter(stream);

            StreamIniDataParser parser = new StreamIniDataParser();

            parser.WriteData(writer, data);

            writer.Close();
        }

        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            string raw = data[section][key];
            bool value = defaultValue;
            bool.TryParse(raw, out value);

            return value;
        }

        public int GetInt(string section, string key, int defaultValue = 0)
        {
            string raw = data[section][key];
            int value = defaultValue;
            int.TryParse(raw, out value);

            return value;
        }

        public float GetFloat(string section, string key, float defaultValue = 0f)
        {
            string raw = data[section][key];
            float value = defaultValue;
            float.TryParse(raw, out value);

            return value;
        }

        public DateTime GetDateTime(string section, string key, DateTime defaultValue = new DateTime())
        {
            string raw = data[section][key];
            DateTime value = defaultValue;
            DateTime.TryParse(raw, out value);

            return value;
        }

        public string GetString(string section, string key, string defaultValue = "")
        {
            string raw = data[section][key];
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;
            return raw;
        }

        public Uri GetPath(string section, string key)
        {
            string raw = data[section][key];

            Uri result;
            if (Uri.TryCreate(file, raw, out result))
                return result;

            return null;
        }

        public Uri GetUri(string section, string key)
        {
            string raw = data[section][key];

            Uri result;
            if (Uri.TryCreate(raw, UriKind.RelativeOrAbsolute, out result))
                return result;

            return null;
        }

        private Uri file;

        private IniData data;

        private static Dictionary<string, ConfigFile> configTable = new Dictionary<string, ConfigFile>();
    }
}
