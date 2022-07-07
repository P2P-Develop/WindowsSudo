using System;
using System.Collections.Generic;

namespace WindowsSudo.Action
{
    public interface IActionBase
    {
        string Name { get; }
        Dictionary<string, Type> Arguments { get; }
        Dictionary<string, dynamic> execute(MainService main, TCPHandler client, Dictionary<string, dynamic> input);
    }
}
