using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.Sockets;

namespace WindowsSudo.Action.Actions
{
    public class GenToken : IActionBase
    {
        public string Name => "generate_token";

        public Dictionary<string, Type> Arguments => new Dictionary<string, Type>
        {
            { "username", typeof(string) }
        };

        public Dictionary<string, dynamic> execute(MainService main, TcpClient client,
            Dictionary<string, dynamic> input)
        {

            SelectQuery query = new SelectQuery("Win32_UserAccount");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject envVar in searcher.Get())
            {
                Debug.WriteLine("Username : {0}", envVar["Name"]);
            }

            string username = input["username"];
            string domain, password;
            if (input.ContainsKey("domain"))
                domain = input["domain"];
            else
                domain = null;
            if (input.ContainsKey("password"))
                password = input["password"];
            else
                password = null;

            if (domain != null && !CredentialHelper.DomainExists(domain))
                return Utils.failure(412, "Could not find domain " + domain);

            if (!CredentialHelper.UserExists(username, domain))
                return Utils.failure(404, "Could not find user " + username);

            if (password == null && CredentialHelper.UserPasswordRequired(username))
                return Utils.failure(401, "Password required.");

            try
            {
                if (!CredentialHelper.ValidateAccount(username, password, domain, true))
                    throw new CredentialHelper.Exceptions.BadPasswordException("Bad credential.");

                TokenManager.TokenInfo token = TokenManager.GenerateToken(username, password, domain);

                return Utils.success(new Dictionary<string, dynamic>
                {
                    { "token", token.Token },
                    { "token_private", token.Token_Priv },
                    { "expires_in", DateTime.Now.Second + token.Duration }
                });

            }
            catch (CredentialHelper.Exceptions.BadPasswordException)
            {
                return Utils.failure(403, "Bad credential.");
            }
            catch (CredentialHelper.Exceptions.DomainNotFoundException)
            {
                return Utils.failure(412, "Could not find domain " + domain);
            }
            catch (CredentialHelper.Exceptions.PasswordRequiredException)
            {
                return Utils.failure(401, "Password required.");
            }
            catch (CredentialHelper.Exceptions.UserNotFoundException)
            {
                return Utils.failure(404, "Could not find user " + username);
            }

        }
    }
}
