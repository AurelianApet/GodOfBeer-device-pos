using GodOfBeer.util;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace com.medipiaenc.vitalsign.CMS.util
{
    class Configuration
    {
        private string iniPath;

        public Configuration(string path)
        {
            this.iniPath = path;
        }

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(
            String section,
            String key,
            String def,
            StringBuilder retVal,
            int size,
            String filePath);

        [DllImport("kernel32.dll")]
        private static extern long WritePrivateProfileString(
            String section,
            String key,
            String val,
            String filePath);


        internal String GetIniValue(String Section, String Key, String defValue = "")
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = Configuration.GetPrivateProfileString(Section, Key, defValue, temp, 1024, iniPath);
            return temp.ToString();
        }

        internal void SetIniValue(String Section, String Key, String Value)
        {
            Configuration.WritePrivateProfileString(Section, Key, Value, iniPath);
        }
    }

    class ConfigurationManager
    {
        internal static ConfigurationManager instance { get; private set; }
        internal Configuration main { get; private set; }
        internal Configuration save { get; private set; }

        static ConfigurationManager()
        {
            ConfigurationManager.instance = new ConfigurationManager();
        }

        private ConfigurationManager()
        {
            string path = Utils.applicationPath;
            this.main = new Configuration(path + @"config.ini");
            this.save = new Configuration(path + @"setting.ini");
        }
    }
}
