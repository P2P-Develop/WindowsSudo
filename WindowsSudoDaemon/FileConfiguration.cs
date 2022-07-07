using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace WindowsSudo
{
    public class FileConfiguration
    {
        private static readonly char DELIMITER = '.';
        private readonly string _filePath;
        private Dictionary<string, dynamic> configuration;

        private Dictionary<string, dynamic> defaultConfig;

        public FileConfiguration(string filePath)
        {
            _filePath = filePath;
            configuration = new Dictionary<string, dynamic>();

            if (File.Exists(filePath))
            {
                if (LoadConfig())
                    Debug.WriteLine("[AutoLoadConfig] Loaded from file " + filePath);
                else
                    Debug.WriteLine("[AutoLoadConfig] Failed to load from file " + filePath);
            }
        }

        public void SaveDefaultConfig(Dictionary<string, dynamic> defaultConfiguration)
        {
            if (File.Exists(_filePath))
                return;

            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(defaultConfiguration);

            File.WriteAllText(_filePath, json);

            if (defaultConfig == null)
                defaultConfig = defaultConfiguration;

            configuration = defaultConfiguration;
        }

        public bool LoadConfig()
        {
            Debug.WriteLine("Loading configuration from " + _filePath);

            if (!File.Exists(_filePath))
                return false;

            var json = File.ReadAllText(_filePath);
            Dictionary<string, dynamic> config = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
            configuration = config;

            return true;
        }

        public void SaveConfig()
        {
            Debug.WriteLine("Saving configuration to " + _filePath);
            var json = JsonConvert.SerializeObject(configuration);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>
        ///     Returns the value of the configuration parameter with the given key.
        ///     Key is separated by dots. Nested key is supported if the non-nested value is a dictionary.
        /// </summary>
        /// <param name="key">Access key</param>
        /// <param name="useDefaultOnNull">Returns default value on null</param>
        /// <param name="source">Set null to use internal config</param>
        /// <returns>Configuration value</returns>
        public dynamic GetConfig(string key, bool useDefaultOnNull = false, Dictionary<string, dynamic> source = null)
        {
            if (source == null)
                source = configuration;

            if (source.ContainsKey(key))
            {
                var value = source[key];

                if (value == null && useDefaultOnNull)
                    return GetConfig(key, false, defaultConfig);
                return value;
            }

            var keys = key.Split(DELIMITER);

            if (keys.Length == 1)
                return null;

            dynamic current = source;

            foreach (var dkey in keys)
            {
                if (current.GetType() != typeof(Dictionary<string, dynamic>) && !current.ContainsKey(dkey))
                    if (useDefaultOnNull)
                        return GetConfig(key, false, defaultConfig);
                    else
                        return null;

                current = current[dkey];
            }

            if (current == null && useDefaultOnNull)
                return GetConfig(key, false, defaultConfig);
            return current;
        }

        public int GetInteger(string key, bool useDefaultOnNull = true)
        {
            var value = GetConfig(key, useDefaultOnNull);

            if (value == null)
                throw new NoNullAllowedException("Configuration value is null: " + key);

            return (int)value;
        }

        public string GetString(string key, bool useDefaultOnNull = true)
        {
            return GetConfig(key, useDefaultOnNull).ToString();
        }

        public bool GetBoolean(string key, bool falseOnNull = false, bool useDefaultOnNull = true)
        {
            var value = GetConfig(key, useDefaultOnNull);

            if (value == null)
                if (falseOnNull)
                    return false;
                else
                    throw new NoNullAllowedException("Configuration value is null: " + key);

            return (bool)value;
        }


        public bool IsNull(string key)
        {
            return GetConfig(key) == null;
        }

        public void SetConfig(string key, dynamic value)
        {
            var keys = key.Split(DELIMITER);

            if (keys.Length == 1)
                configuration[key] = value;

            dynamic current = configuration;

            for (var keyIndex = 0; keyIndex < keys.Length - 1; keyIndex++)
            {
                var dkey = keys[keyIndex];
                if (!current.ContainsKey(dkey))
                    current[dkey] = new Dictionary<string, dynamic>();
                if (keyIndex == keys.Length - 2)
                    current[dkey][keys[keyIndex + 1]] = value;
                else
                    current = current[dkey];
            }
        }
    }
}
