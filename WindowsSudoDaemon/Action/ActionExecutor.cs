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
            return actions[name].execute(main, client, args);
        }
    }
}
