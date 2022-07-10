using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WindowsSudo.Sudoers
{
    public class Sudoer
    {
        public string ExecuteUser { get; set; }
        public bool IsExecuteUserIsGroup { get; set; }

        public string ExecuteHost { get; set; }

        public List<ExecuteAsUser> ExecuteAsUser { get; set; }

        public List<Tag> Tags { get; set; }

        public List<Command> Commands { get; set; }

        public bool IsAllowed(string username, string userGroup, string host, string targetUsername, string targetGroup,
            string fullExecutablePath, Sudoers parentSudoers)
        {
            Dictionary<string, Alias> userAliases = parentSudoers.UserAliases;
            Dictionary<string, Alias> runAsAliases = parentSudoers.RunAsAliases;
            Dictionary<string, Alias> hostAliases = parentSudoers.HostAliases;
            Dictionary<string, Alias> commandAliases = parentSudoers.CommandAliases;

            if (IsExecuteUserIsGroup)
            {
                if (!(TestAllowed(ExecuteUser, targetGroup) ||
                      TestAllowedByAlias(userAliases, ExecuteUser, targetGroup)))
                    return false;
            }
            else if (!(TestAllowed(ExecuteUser, username) || TestAllowedByAlias(userAliases, ExecuteUser, userGroup)))
            {
                return false;
            }

            if (!(TestAllowed(ExecuteHost, host) || TestAllowedByAlias(hostAliases, ExecuteHost, host)))
                return false;

            foreach (ExecuteAsUser runAsUser in ExecuteAsUser)
            {
                if (!(TestAllowed(runAsUser.User, targetUsername) ||
                      TestAllowedByAlias(runAsAliases, runAsUser.User, targetUsername)))
                    return false;
                if (!(TestAllowed(runAsUser.Group, targetGroup) ||
                      TestAllowedByAlias(runAsAliases, runAsUser.User, targetGroup)))
                    return false;
            }

            var commandAllow = false;
            foreach (Command command in Commands)
            {
                if (command.Name == "ALL")
                {
                    commandAllow = command.IsAllowed;
                    continue;
                }

                var commandName = Path.GetFileNameWithoutExtension(fullExecutablePath);

                if (command.Name == commandName || command.Name == fullExecutablePath ||
                    TestAllowedByAlias(commandAliases, command.Name, commandName) ||
                    TestAllowedByAlias(commandAliases, command.Name, fullExecutablePath))
                    commandAllow = command.IsAllowed;
            }

            return commandAllow;
        }

        public static bool TestAllowed(string setting, string testString)
        {
            return setting == "ALL" || setting == testString;
        }

        public static bool TestAllowedByAlias(Dictionary<string, Alias> aliases, string settingName, string testString)
        {
            return aliases.Any(x => x.Key.ToLower() == settingName.ToLower() &&
                                    x.Value.AliasValues.Any(y =>
                                        (y.Value == "ALL" || y.Value == testString) && y.Allowed
                                    ));
        }

        public static Sudoer Empty()
        {
            Sudoer sudoer = new Sudoer();

            sudoer.ExecuteUser = "";
            sudoer.IsExecuteUserIsGroup = false;

            sudoer.ExecuteHost = "";

            sudoer.ExecuteAsUser = new List<ExecuteAsUser>();

            sudoer.Tags = new List<Tag>();

            sudoer.Commands = new List<Command>();

            return sudoer;
        }

        public Sudoer normalize()
        {
            if (ExecuteUser.Length == 0)
                ExecuteUser = "ALL";
            if (ExecuteHost.Length == 0)
                ExecuteHost = "ALL";

            return this;
        }
    }

    public class ExecuteAsUser
    {
        public ExecuteAsUser()
        {
            User = "";
            Group = "";
        }

        public string User { get; set; }
        public string Group { get; set; }
    }

    public class Command
    {
        public Command(string name, bool isAllowed)
        {
            Name = name;
            IsAllowed = isAllowed;
        }

        public string Name { get; set; }
        public bool IsAllowed { get; set; }
    }

    public class Tag
    {
        public enum TagType
        {
            EXEC,
            NOEXEC,
            FOLLOW,
            NOFOLLOW,
            LOG_INPUT,
            NOLOG_INPUT,
            LOG_OUTPUT,
            NOLOG_OUTPUT,
            MAIL,
            NOMAIL,
            PASSWD,
            NOPASSWD,
            SETENV,
            NOSETENV,

            NONE
        }

        public Tag()
        {
            Type = TagType.NONE;
            IsEnabled = true;
        }

        public TagType Type { get; set; }
        public bool IsEnabled { get; set; }

        public static bool IsTagEnabled(TagType type, List<Tag> tags)
        {
            return tags.Any(x => x.Type == type && x.IsEnabled);
        }

        public static bool IsStartsWithTagExists(string startsWith)
        {
            return Enum.GetNames(typeof(TagType)).Any(x => x.StartsWith(startsWith));
        }

        public static TagType parseType(string name)
        {
            try
            {
                return (TagType)Enum.Parse(typeof(TagType), name);
            }
            catch (Exception)
            {
                return TagType.NONE;
            }
        }
    }
}
