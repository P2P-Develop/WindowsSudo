using System;
using System.Collections.Generic;

namespace WindowsSudo
{
    public class Utils
    {
        /// <summary>
        /// Returns a dictionary of missing argument names or invalid argument names.
        /// </summary>
        /// <param name="required">Required arguments dictionary,</param>
        /// <param name="args">Provided arguments dictionary.</param>
        /// <returns></returns>
        public static List<string> checkArgs(Dictionary<string, Type> required,
            Dictionary<string, dynamic> args,
            List<string> allowNull = null)
        {
            List<string> missing = new List<string>();

            foreach (KeyValuePair<string, Type> kvp in required)
            {

                if (!args.ContainsKey(kvp.Key))
                {
                    Console.WriteLine("Missing argument: " + kvp.Key);
                    missing.Add(kvp.Key);
                    continue;
                }

                if (args[kvp.Key].GetType() != kvp.Value)
                {
                    Console.WriteLine("Invalid argument: " + kvp.Key);
                    missing.Add(kvp.Key);
                    continue;
                }

                if (allowNull != null && allowNull.Contains(kvp.Key) && args[kvp.Key] == null)
                {
                    Console.WriteLine("Invalid argument: " + kvp.Key);
                    missing.Add(kvp.Key);
                }
            }

            return missing;
        }

        public static Dictionary<string, dynamic> failure(int code, string message) => new Dictionary<string, dynamic>()
        {
            { "success", false },
            { "code", code },
            { "message", message }
        };

        public static Dictionary<string, dynamic> success() => new Dictionary<string, dynamic>()
        {
            { "success", true }
        };

        public static Dictionary<string, dynamic> success(string message) => new Dictionary<string, dynamic>()
        {
            { "success", true },
            { "message", message }
        };

        public static Dictionary<string, dynamic> success(string message, dynamic data) => new Dictionary<string, dynamic>()
        {
            { "success", true },
            { "message", message },
            { "data", data }
        };
    }
}
