using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace WindowsSudo.Action.Actions
{
    public class Sudo : IActionBase
    {
        public string Name => "sudo";

        public Dictionary<string, Type> Arguments => new Dictionary<string, Type>
        {
            {"command", typeof(string)},
            {"args", typeof(string[])},
            {"new_window", typeof(bool)},
            {"workdir", typeof(string)},
            {"env", typeof(Dictionary<string, string>)},
            {"user", typeof(string)}
        };

        public Dictionary<string, dynamic> execute(MainService main, TcpClient client, Dictionary<string, dynamic> input)
        {
            string workdir = input["workdir"];
            string command = input["command"];
            string[] args = p(input["args"]);
            bool new_window = input["new_window"];
            Dictionary<string, string> env = p(input["env"]);

            ProcessManager.ProcessCause cause =
                main.processManager.StartProcess(workdir, command, args, new_window, env);
            if (cause == ProcessManager.ProcessCause.Success)
                return Utils.success("Process created");
            return Utils.failure(114, "a");
        }

        private static string[] p(List<Object> o)
        {
            return o.Cast<string>().ToArray();
        }

        private static Dictionary<string, string> p(Dictionary<string, object> o)
        {
            return o.ToDictionary(x => x.Key, x => x.Value.ToString());
        }



    }
}
