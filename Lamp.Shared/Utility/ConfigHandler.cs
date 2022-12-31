using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace Lamp
{
    public class ConfigHandler
    {
        private Dictionary<string, string> _configs = new Dictionary<string, string>();
        private string ConfigFile => Path.Combine(_genieDirectory, "Config", "settings.cfg");
        private static readonly string ConfigLineBeginning = "#Config {";
        public string[] LaunchParameters { get; set; }
        public Dictionary<string, string> Configs 
        {
            get
            {
                return _configs;
            }
            set
            {
                _configs = value;
            }
        } 
        private string _genieDirectory;
        public string GenieDirectory {
            get
            {
                return _genieDirectory;
            }
            set
            {
                if( _genieDirectory != value)
                {
                    _genieDirectory = value;
                }
                Load();
            }
        } 

        private static ConfigHandler _instance = null;
        public static ConfigHandler Instance 
        {
            get 
            { 
                if(_instance == null)
                {
                    _instance = new ConfigHandler();
                }
                return _instance;
            }
        }

        private ConfigHandler()
        {
            _genieDirectory = FileHandler.LocalDirectory;
        }
        
        public string GetConfig(string ConfigName)
        {
            if (Configs.ContainsKey(ConfigName)) return Instance.Configs[ConfigName];
            return string.Empty;
        }

        public void SetConfig(string ConfigName, string ConfigValue)
        {
            if (Configs.ContainsKey(ConfigName)) Configs[ConfigName] = ConfigValue;
            else Configs.Add(ConfigName, ConfigValue);
        }

        public async Task<bool> Save()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> config in Configs)
                {

                    lines.Add(ConfigLineBeginning + config.Key + "} {" + config.Value + "}");
                }
                
                return await FileHandler.SaveTextToFile(lines, ConfigFile);
            }
            catch(Exception ex)
            {
                return false;
            }

        }
        public async Task<bool> Load()
        {
            try
            {
                List<string> lines = await FileHandler.GetFileLines(ConfigFile);
                Dictionary<string, string> Configs = new Dictionary<string, string>();
                foreach(string line in lines) 
                {
                    string[] config = line.Split("} {",StringSplitOptions.TrimEntries);
                        
                    if (!config[0].ToUpper().StartsWith(ConfigLineBeginning.ToUpper()) || config.Length < 2) continue;

                    string configName = config[0].Substring(ConfigLineBeginning.Length);
                    if (!Configs.ContainsKey(configName))
                    {
                        Configs.Add(configName, config[1].Substring(0, config[1].Length - 1));
                    }
                }
                _configs = Configs;
                return true;
            }catch(Exception ex) 
            { 
                return false; 
            }
        }

        public static void SaveConfig(string GenieDirectory, string ConfigName, string ConfigValue)
        {
            
        }
    }
}
