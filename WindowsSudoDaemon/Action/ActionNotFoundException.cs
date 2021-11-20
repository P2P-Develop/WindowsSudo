using System;

namespace WindowsSudo.Action
{
    public class ActionNotFoundException : Exception
    {
        public ActionNotFoundException(string message) : base(message)
        {
        }
    }
}
