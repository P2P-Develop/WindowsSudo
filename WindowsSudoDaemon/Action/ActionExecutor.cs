using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace WindowsSudo.Action
{
    public class ActionExecutor
    {
        private Dictionary<string, IActionBase> actions;
        private MainService main;

        public ActionExecutor(MainService main)
        {
            this.main = main;
            actions = new Dictionary<string, IActionBase>();
        }

        public void registerAction(IActionBase action)
        {
            if (actions.ContainsKey(action.Name))
                return;
            actions.Add(action.Name, action);
        }

        public Dictionary<string, dynamic> executeAction(string name, TcpClient client, Dictionary<string, dynamic> args)
        {
            if (!actions.ContainsKey(name))
                throw new ActionNotFoundException("Action not found.");

            IActionBase action = actions[name];

            if (action.Arguments != null)
            {
                if (!args.ContainsKey("args"))
                    throw new ArgumentException("Action requires arguments.");

                List<string> missingKeys;
                if ((missingKeys = Utils.checkArgs(action.Arguments, args["args"])).Count > 1)
                    throw new ArgumentException("Missing arguments: " + string.Join(", ", missingKeys));

                return action.execute(main, client, args["args"]);
            }

            return action.execute(main, client, new Dictionary<string, dynamic>());
        }
    }
}
