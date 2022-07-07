using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WindowsSudo.Action
{
    public class ActionExecutor
    {
        private readonly Dictionary<string, IActionBase> actions;
        private readonly MainService main;

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

        public Dictionary<string, dynamic> executeAction(string name, TCPHandler client,
            Dictionary<string, dynamic> args)
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

                return action.execute(main, client, ConvertJObject(args["args"]));
            }

            return action.execute(main, client, new Dictionary<string, dynamic>());
        }

        private static dynamic ConvertJObject(dynamic dyn)
        {
            if (dyn.GetType() == typeof(JObject))
            {
                Dictionary<string, dynamic> response = new Dictionary<string, dynamic>();
                foreach (var prop in dyn)
                    if (prop.GetType() == typeof(JProperty))
                        response.Add(prop.Name, ConvertJObject(prop.Value));
                    else
                        response.Add(prop.Key, ConvertJObject(prop.Value));
                return response;
            }

            if (dyn.GetType() == typeof(JArray))
            {
                List<dynamic> response = new List<dynamic>();
                foreach (var item in dyn)
                    response.Add(ConvertJObject(item));
                return response;
            }

            if (dyn.GetType() == typeof(JValue))
                return ((JValue)dyn).Value;

            return dyn;
        }
    }
}
