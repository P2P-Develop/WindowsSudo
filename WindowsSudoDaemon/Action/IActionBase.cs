using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace WindowsSudo.Action
{
    public interface IActionBase
    {
        string Name { get; }
        Dictionary<string, Type> Arguments { get; }
        Dictionary<string, dynamic> execute(MainService main, TcpClient client, Dictionary<string, dynamic> input);
    }
}
