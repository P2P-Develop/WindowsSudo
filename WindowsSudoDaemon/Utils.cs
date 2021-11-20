using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace WindowsSudo
{
    public class Utils
    {
        /// <summary>
        ///     Returns a dictionary of missing argument names or invalid argument names.
        /// </summary>
        /// <param name="required">Required arguments dictionary,</param>
        /// <param name="args">Provided arguments dictionary.</param>
        /// <returns></returns>
        public static List<string> checkArgs(Dictionary<string, Type> required,
            Dictionary<string, object> args,
            List<string> allowNull = null)
        {
            List<string> missing = new List<string>();

            foreach (KeyValuePair<string, Type> kvp in required)
            {
                if (!args.ContainsKey(kvp.Key))
                {
                    Debug.WriteLine("Missing argument: " + kvp.Key);
                    missing.Add(kvp.Key);
                    continue;
                }

                if (args[kvp.Key].GetType() != kvp.Value)
                {
                    Debug.WriteLine("Invalid argument: " + kvp.Key);
                    missing.Add(kvp.Key);
                    continue;
                }

                if (allowNull != null && allowNull.Contains(kvp.Key) && args[kvp.Key] == null)
                {
                    Debug.WriteLine("Invalid argument: " + kvp.Key);
                    missing.Add(kvp.Key);
                }
            }

            return missing;
        }

        public static List<string> checkArgs(Dictionary<string, Type> required, JObject obj)
        {
            List<string> missing = new List<string>();

            foreach (KeyValuePair<string, Type> kvp in required)
            {
                if (!obj.ContainsKey(kvp.Key))
                {
                    Debug.WriteLine("Missing argument: " + kvp.Key);
                    missing.Add(kvp.Key);
                    continue;
                }

                try
                {
                    if (obj[kvp.Key].Value<dynamic>() is JObject)
                        if (kvp.Value.IsGenericType && kvp.Value.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                            continue;
                    if (obj[kvp.Key].Value<dynamic>() is JArray)
                        if (kvp.Value.IsArray)
                            continue;

                    Convert.ChangeType(obj[kvp.Key].Value<dynamic>(), kvp.Value);
                }
                catch (InvalidCastException)
                {
                    Debug.WriteLine("Invalid argument: " + kvp.Key);
                    missing.Add(kvp.Key);
                }
            }

            return missing;
        }

        public static string ResolvePath(string name, string workingDir, string env_path, string env_path_ext)
        {
            var p = workingDir + ";" + env_path + ";";
            var search_paths = p.Split(';');

            var isdir = false;

            foreach (var path in search_paths)
            {
                var full_path = Path.Combine(path, name);

                if (File.Exists(full_path))
                    if (IsFile(full_path))
                        return full_path;
                    else
                        isdir = true;
                foreach (var ext in env_path_ext.Split(';'))
                {
                    var exp = full_path + ext;
                    if (File.Exists(exp))
                        if (IsFile(exp))
                        {
                            return exp;
                        }
                        else
                        {
                            isdir = true;
                            return exp;
                        }
                }
            }

            if (isdir)
                return "";

            return null;
        }

        public static bool IsFile(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.Archive) != 0;
        }

        public static Dictionary<string, dynamic> failure(int code, string message)
        {
            return new Dictionary<string, dynamic>
            {
                { "success", false },
                { "code", code },
                { "message", message }
            };
        }

        public static Dictionary<string, dynamic> success()
        {
            return new Dictionary<string, dynamic>
            {
                { "success", true }
            };
        }

        public static Dictionary<string, dynamic> success(string message)
        {
            return new Dictionary<string, dynamic>
            {
                { "success", true },
                { "message", message }
            };
        }

        public static Dictionary<string, dynamic> success(string message, dynamic data)
        {
            return new Dictionary<string, dynamic>
            {
                { "success", true },
                { "message", message },
                { "data", data }
            };
        }

        public static Dictionary<string, dynamic> success(dynamic data)
        {
            return new Dictionary<string, dynamic>
            {
                { "success", true },
                { "message", "OK" },
                { "data", data }
            };
        }
    }
}
