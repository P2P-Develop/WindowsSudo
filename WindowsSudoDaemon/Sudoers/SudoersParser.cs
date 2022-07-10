using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WindowsSudo.Sudoers
{
    public class SudoersParser
    {
        private readonly Regex regex = new Regex(@"^\w+\s");

        private SudoersParser()
        {
            sudoers = new Sudoers();
        }

        private Sudoers sudoers { get; }

        public static Sudoers ParseFile(string filePath)
        {
            SudoersParser parser = new SudoersParser();
            parser.ParseFile1(filePath);
            return parser.sudoers;
        }

        private void ParseFile1(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    ParseLine(line);
            }
        }

        private void ParseLine(string line)
        {
            LineType lineType = DetectLineType(line);

            if (lineType == LineType.UNKNOWN)
                throw new NotSupportedException("Line type not supported: " + line);
            if (lineType == LineType.COMMENT || lineType == LineType.EMPTY_LINE)
                return;

            if (lineType == LineType.ENTRY)
            {
                Sudoer sudoer = new SudoerParser().ParseSudoerLine(line);
                sudoers.SudoersList.Add(sudoer);
                return;
            }

            if (!(lineType == LineType.USER_ALIAS || lineType == LineType.RUNAS_ALIAS ||
                  lineType == LineType.HOST_ALIAS || lineType == LineType.COMMAND_ALIAS))
                throw new Exception("Oops! The application went wrong. You cannot be here.");

            Alias alias = new AliasParser().ParseAliasLine(line);

            switch (lineType)
            {
                case LineType.USER_ALIAS:
                    sudoers.UserAliases.Add(alias.AliasName, alias);
                    break;
                case LineType.RUNAS_ALIAS:
                    sudoers.RunAsAliases.Add(alias.AliasName, alias);
                    break;
                case LineType.HOST_ALIAS:
                    sudoers.HostAliases.Add(alias.AliasName, alias);
                    break;
                case LineType.COMMAND_ALIAS:
                    sudoers.CommandAliases.Add(alias.AliasName, alias);
                    break;
            }
        }

        private LineType DetectLineType(string line)
        {
            if (line.StartsWith("#"))
                return LineType.COMMENT;
            if (line.StartsWith("User_Alias"))
                return LineType.USER_ALIAS;
            if (line.StartsWith("Runas_Alias"))
                return LineType.RUNAS_ALIAS;
            if (line.StartsWith("Host_Alias"))
                return LineType.HOST_ALIAS;
            if (line.StartsWith("Command_Alias"))
                return LineType.COMMAND_ALIAS;
            if (line.All(c => c == ' ' || c == '\t' || c == '\r' || c == '\n'))
                return LineType.EMPTY_LINE;
            if (regex.IsMatch(line))
                return LineType.ENTRY;
            return LineType.UNKNOWN;
        }

        private enum LineType
        {
            COMMENT,

            USER_ALIAS,
            RUNAS_ALIAS,
            HOST_ALIAS,
            COMMAND_ALIAS,

            EMPTY_LINE,

            ENTRY,

            UNKNOWN
        }
    }

    public class AliasParser
    {
        public AliasParser()
        {
            phase = Phase.PREPARE;
        }

        private Phase phase { get; set; }

        public Alias ParseAliasLine(string line)
        {
            phase = Phase.CHECK_ALIAS_LINE; // First phase

            var tempAliasName = "";


            var tempAliasItemName = "";
            var tempAliasItemAllowed = true;
            List<Alias.AliasItem> aliasValues = new List<Alias.AliasItem>();

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == ' ')
                    NextPhase();

                switch (phase)
                {
                    case Phase.CHECK_ALIAS_LINE: // Nothing to do.
                        break;
                    case Phase.PARSE_NAME:
                        if (c == '=')
                        {
                            NextPhase();
                            break;
                        }

                        tempAliasName += c;
                        break;
                    case Phase.PARSE_ITEMS:
                        if (c == ',')
                        {
                            aliasValues.Add(new Alias.AliasItem(tempAliasItemAllowed, tempAliasItemName));
                            tempAliasItemName = "";
                            tempAliasItemAllowed = true;
                        }
                        else if (c == '!')
                        {
                            tempAliasItemAllowed = false;
                        }
                        else
                        {
                            tempAliasItemName += c;
                        }

                        break;
                }
            }

            if (tempAliasItemName.Length != 0)
                aliasValues.Add(new Alias.AliasItem(tempAliasItemAllowed, tempAliasItemName));

            return new Alias(tempAliasName, aliasValues.ToArray());
        }


        private void NextPhase()
        {
            try
            {
                phase = (Phase)(int)phase + 1;
            }
            catch (Exception)
            {
                phase = Phase.FINISHED;
            }
        }

        private enum Phase
        {
            PREPARE,
            CHECK_ALIAS_LINE,
            PARSE_NAME,
            PARSE_ITEMS,
            FINISHED
        }
    }

    public class SudoerParser
    {
        public SudoerParser()
        {
            phase = Phase.PREPARE;
        }

        private Phase phase { get; set; }

        public Sudoer ParseSudoerLine(string line)
        {
            Sudoer sudoer = Sudoer.Empty();

            phase = Phase.PARSE_USER_NAME; // First phase

            ExecuteAsUser tempExecuteAsUser = new ExecuteAsUser();

            var tempTagName = "";
            Tag tempTag = new Tag();

            var tempCommandName = "";
            var tempCommandIsAllowed = true;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (phase != Phase.PARSE_USER_NAME &&
                    (c == ' ' ||
                     c == '\t'))
                    continue;

                switch (phase)
                {
                    case Phase.PARSE_USER_NAME:
                        switch (c)
                        {
                            case ' ':
                            case '\t':
                                if (sudoer.ExecuteUser.Length > 0)
                                    NextPhase();
                                break;
                            case '%':
                                if (sudoer.ExecuteUser.Length == 0)
                                    sudoer.IsExecuteUserIsGroup = true;
                                break;
                            default:
                                sudoer.ExecuteUser += c;
                                break;
                        }

                        break;
                    case Phase.PARSE_HOST:
                        switch (c)
                        {
                            case '=':
                                if (sudoer.ExecuteHost.Length == 0)
                                    NextPhase();
                                break;
                            default:
                                sudoer.ExecuteHost += c;
                                break;
                        }

                        break;
                    case Phase.PARSE_RUN_AS:
                        switch (c)
                        {
                            case '(':
                                break;
                            case ',':
                                sudoer.ExecuteAsUser.Add(tempExecuteAsUser);
                                tempExecuteAsUser = new ExecuteAsUser();
                                break;
                            case ')':
                                if (tempExecuteAsUser.User.Length != 0)
                                    sudoer.ExecuteAsUser.Add(tempExecuteAsUser);
                                phase = Phase.PARSE_TAGS;
                                break;
                            case ':':
                                NextPhase();
                                break;
                            default:
                                tempExecuteAsUser.User += c;
                                break;
                        }

                        break;
                    case Phase.PARSE_RUN_AS_GROUP:
                        switch (c)
                        {
                            case ',':
                                sudoer.ExecuteAsUser.Add(tempExecuteAsUser);
                                tempExecuteAsUser = new ExecuteAsUser();
                                break;
                            case ')':
                                if (tempExecuteAsUser.User.Length != 0)
                                    sudoer.ExecuteAsUser.Add(tempExecuteAsUser);
                                NextPhase();
                                break;
                            default:
                                tempExecuteAsUser.Group += c;
                                break;
                        }

                        break;
                    case Phase.PARSE_TAGS:
                        switch (c)
                        {
                            case '!':
                                tempTag.IsEnabled = false;
                                break;
                            case ',':
                                if (Tag.IsStartsWithTagExists(tempTagName))
                                {
                                    sudoer.Tags.Add(tempTag);
                                    tempTag = new Tag();
                                }
                                else
                                {
                                    i -= tempTagName.Length;
                                    NextPhase();
                                }

                                break;
                            case ':':
                                if (Tag.IsStartsWithTagExists(tempTagName))
                                {
                                    sudoer.Tags.Add(tempTag);
                                }
                                else
                                {
                                    i -= tempTagName.Length;
                                    NextPhase();
                                }

                                break;
                            default:
                                tempTagName += c;
                                break;
                        }

                        break;
                    case Phase.PARSE_COMMANDS:
                        switch (c)
                        {
                            case ',':
                                if (line[i - 1] == '\\' && line[i - 2] != '\\')
                                {
                                    tempCommandName += c;
                                }
                                else
                                {
                                    sudoer.Commands.Add(new Command(tempCommandName, tempCommandIsAllowed));
                                    tempCommandName = "";
                                    tempCommandIsAllowed = true;
                                }

                                break;
                            case '!':
                                if (tempCommandName.Length == 0)
                                    tempCommandIsAllowed = false;
                                break;
                            default:
                                tempCommandName += c;
                                break;
                        }

                        break;
                }
            }

            return sudoer;
        }

        private void NextPhase()
        {
            try
            {
                phase = (Phase)(int)phase + 1;
            }
            catch (Exception)
            {
                phase = Phase.FINISHED;
            }
        }

        private enum Phase
        {
            PREPARE,
            PARSE_USER_NAME,
            PARSE_HOST,
            PARSE_RUN_AS,
            PARSE_RUN_AS_GROUP,
            PARSE_TAGS,
            PARSE_COMMANDS,
            FINISHED
        }
    }
}
