using System;
using System.Collections.Generic;
using CommandLine;

namespace WindowsSudoClient
{
    public class CommandLineOption
    {
        private static readonly string[] TEXT_ARGUMENTS =
        {
            "u", "user"
        };

        [Option('u', "user", Required = false, HelpText = "User name to run the command as.")]
        public string UserName { get; }

        public string Command { get; set; }
        public string Arguments { get; set; }

        public static CommandLineOption Parse(string[] args)
        {
            List<string> argumentsWillBeParsed = new List<string>();
            List<string> commandArguments = new List<string>();

            var isTextArgument = false;

            string command = null;

            foreach (var arg in args)
                if (command != null)
                {
                    commandArguments.Add(arg);
                }
                else if (arg.StartsWith("-"))
                {
                    argumentsWillBeParsed.Add(arg);
                    if (Array.IndexOf(TEXT_ARGUMENTS, arg.Substring(1)) != -1)
                        isTextArgument = true;
                }
                else if (isTextArgument)
                {
                    argumentsWillBeParsed.Add(arg);
                    isTextArgument = false;
                }
                else
                {
                    command = arg;
                }


            CommandLineOption option = null;
            Parser.Default.ParseArguments<CommandLineOption>(argumentsWillBeParsed.ToArray())
                .WithParsed(o => option = o);

            if (option == null)
                return null;

            option.Command = command;
            option.Arguments = string.Join(" ", commandArguments);

            return option;
        }
    }
}
