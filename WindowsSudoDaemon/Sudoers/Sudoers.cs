using System.Collections.Generic;

namespace WindowsSudo.Sudoers
{
    public class Sudoers
    {
        public Sudoers()
        {
            UserAliases = new Dictionary<string, Alias>();
            RunAsAliases = new Dictionary<string, Alias>();
            HostAliases = new Dictionary<string, Alias>();
            CommandAliases = new Dictionary<string, Alias>();
            SudoersList = new List<Sudoer>();
        }

        public Dictionary<string, Alias> UserAliases { get; }
        public Dictionary<string, Alias> RunAsAliases { get; }
        public Dictionary<string, Alias> HostAliases { get; }
        public Dictionary<string, Alias> CommandAliases { get; }

        public List<Sudoer> SudoersList { get; }

        public bool IsAllowed(string username, string userGroup, string host, string targetUsername, string targetGroup,
            string executableFullPath)
        {
            foreach (Sudoer sudoer in SudoersList)
                if (sudoer.IsAllowed(username, userGroup, host, targetUsername, targetGroup, executableFullPath, this))
                    return true;

            return false;
        }
    }

    public class Alias
    {
        public Alias(string aliasName, AliasItem[] aliasValues)
        {
            AliasName = aliasName;
            AliasValues = aliasValues;
        }

        public string AliasName { get; set; }
        public AliasItem[] AliasValues { get; set; }

        public class AliasItem
        {
            public AliasItem(bool allowed, string value)
            {
                Allowed = allowed;
                Value = value;
            }

            public bool Allowed { get; set; }
            public string Value { get; set; }
        }
    }
}
