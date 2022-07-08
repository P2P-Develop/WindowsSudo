using System;
using System.Collections.Generic;
using System.Reflection;

namespace WindowsSudo.Action.Actions
{
    public class Info : IActionBase
    {
        public string Name => "info";

        public Dictionary<string, Type> Arguments => null;

        public Dictionary<string, dynamic> execute(MainService main, TCPHandler client,
            Dictionary<string, dynamic> input)
        {
            if (!client.IsLoggedIn())
                return Utils.failure(401, "Not logged in");

            return Utils.success(new Dictionary<string, dynamic>
            {
                { "version", Assembly.GetExecutingAssembly().GetName().Version.ToString() }
            });
        }
    }
}
