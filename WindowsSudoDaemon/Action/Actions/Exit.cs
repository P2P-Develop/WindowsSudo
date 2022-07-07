using System;
using System.Collections.Generic;

namespace WindowsSudo.Action.Actions
{
    public class Exit : IActionBase
    {
        public string Name => "exit";

        public Dictionary<string, Type> Arguments => null;

        public Dictionary<string, dynamic> execute(MainService main, TCPHandler client, Dictionary<string, dynamic> _)
        {
            main.Stop();
            return Utils.success();
        }
    }
}
