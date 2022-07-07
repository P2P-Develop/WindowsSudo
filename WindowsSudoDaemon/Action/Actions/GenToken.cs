using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            Debug.WriteLine("[GenerateToken] Enumerating users...");
            foreach (ManagementObject envVar in searcher.Get())
                Debug.WriteLine("[GenerateToken] Username : {0}", envVar["Name"]);

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

            if (username == null)
                return Utils.failure(400, "Required credentials.");

            if (!CheckCredential(domain, username, password)) return Utils.failure(403, "Bad credential.");

            Debug.Write("[GenerateToken] Generating token...");
            TokenManager.TokenInfo token = TokenManager.GenerateToken(username, password, domain);
            Debug.WriteLine("DONE");

            return Utils.success(new Dictionary<string, dynamic>
            {
                { "token", token.Token },
                { "token_private", token.Token_Priv },
                { "expires_in", DateTime.Now.Second + token.Duration }
            });
        }

        private bool CheckCredential(string domain, string username, string password)
        {
            Debug.WriteLine("[GenerateToken] Checking provided credential is valid...");

            try
            {
                CredentialHelper.ValidateAccount(username, password, domain, true);
                // Usually, ValidateAccount() returns boolean, but it always returns true because if validation fails, they throw an exception.
                Debug.WriteLine("[GenerateToken] Congratulations, they has passed all tests!");
                return true;
            }
            catch (CredentialHelper.Exceptions.BadPasswordException)
            {
                Debug.WriteLine(
                    "[GenerateToken] Domain exists(or not specified), user exists, but password is wrong. hmm...");
                return false;
            }
            catch (CredentialHelper.Exceptions.DomainNotFoundException)
            {
                Debug.WriteLine("[GenerateToken] Domain does not exist.");
                return false;
            }
            catch (CredentialHelper.Exceptions.PasswordRequiredException)
            {
                Debug.WriteLine("[GenerateToken] Password is required, but not provided.");
                return false;
            }
            catch (CredentialHelper.Exceptions.UserNotFoundException)
            {
                Debug.WriteLine("[GenerateToken] User does not exist.");
                return false;
            }
        }
    }
}
