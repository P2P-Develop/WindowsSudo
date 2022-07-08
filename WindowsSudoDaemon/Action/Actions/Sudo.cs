using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;

namespace WindowsSudo.Action.Actions
{
    public class Sudo : IActionBase
    {
        public string Name => "sudo";

        public Dictionary<string, Type> Arguments => new Dictionary<string, Type>
        {
            { "command", typeof(string) },
            { "args", typeof(string[]) },
            { "new_window", typeof(bool) },
            { "workdir", typeof(string) },
            { "env", typeof(Dictionary<string, string>) }
        };

        public Dictionary<string, dynamic> execute(MainService main, TCPHandler client,
            Dictionary<string, dynamic> input)
        {
            if (!client.IsLoggedIn())
                return Utils.failure(401, "Not logged in");

            string workdir = input["workdir"];
            string command = input["command"];
            string[] args = p(input["args"]);
            bool new_window = input["new_window"];
            Dictionary<string, string> env = p(input["env"]);

            try
            {
                ProcessManager.ProcessInfo process =
                    main.processManager.StartProcess(workdir, command, args, new_window, env, client.token);
                return Utils.success("Process created", new Dictionary<string, object>
                {
                    { "pid", process.Id },
                    { "path", process.FullPath }
                });
            }
            catch (InvalidCredentialException)
            {
                TokenManager.InvalidateToken(client.token.Token);
                return Utils.failure(403, "Bad credential.");
            }
            catch (FileNotFoundException)
            {
                return Utils.failure(404, "Executable not found.");
            }
            catch (TypeAccessException)
            {
                return Utils.failure(412, "Is a directory.");
            }
            catch (Exception e)
            {
                return Utils.failure(500, e.Message);
            }
        }

        private static string[] p(List<object> o)
        {
            return o.Cast<string>().ToArray();
        }

        private static Dictionary<string, string> p(Dictionary<string, object> o)
        {
            return o.ToDictionary(x => x.Key, x => x.Value.ToString());
        }
    }
}
