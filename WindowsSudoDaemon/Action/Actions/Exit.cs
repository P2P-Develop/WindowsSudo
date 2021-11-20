using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace WindowsSudo.Action.Actions
{
    public class Exit : IActionBase
    {
        public string Name => "exit";

        public Dictionary<string, Type> Arguments => null;

        public Dictionary<string, dynamic> execute(MainService main, TcpClient client, Dictionary<string, dynamic> _)
        {
            main.Stop();
            return Utils.success();
        }
    }
}
