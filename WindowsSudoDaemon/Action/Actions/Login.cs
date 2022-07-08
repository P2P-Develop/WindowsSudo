using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WindowsSudo.Action.Actions
{
    public class Login : IActionBase
    {
        public string Name => "login";

        public Dictionary<string, Type> Arguments => new Dictionary<string, Type>();

        public Dictionary<string, dynamic> execute(MainService main, TCPHandler client,
            Dictionary<string, dynamic> input)
        {
            if (client.IsLoggedIn())
                return Utils.failure(406, "Already logged in");

            if (input.ContainsKey("token") && input.ContainsKey("token_private"))
                return LoginWithToken(main, client, input);
            if (input.ContainsKey("username"))
                return LoginWithCredentials(main, client, input);
            else
                return Utils.failure(400, "Credentials required.");
        }

        private Dictionary<string, dynamic> LoginWithToken(MainService main, TCPHandler handler,
            Dictionary<string, dynamic> input)
        {
            string token = input["token"];
            string token_private = input["token_private"];

            LoginManager.TokenLoginResult result = main.loginManager.LoginWithToken(handler, token, token_private);

            if (result.Success)
                return Utils.success();

            if (result.Cause == LoginManager.LoginCause.INVALID_TOKEN) return Utils.failure(400, "Bad credential.");

            if (result.Cause == LoginManager.LoginCause.RATE_LIMIT_EXCEEDED)
            {
                return new Dictionary<string, dynamic>
                {
                    { "success", false },
                    { "code", 429 },
                    { "message", "Rate limit exceed." },
                    { "rate", result.CurrentRate }
                };
            }
            else
            {
                Debug.WriteLine("[Login] Unknown error: " + result.Cause);
                return Utils.failure(500, "Unknown error.");
            }
        }

        private Dictionary<string, dynamic> LoginWithCredentials(MainService main, TCPHandler handler,
            Dictionary<string, dynamic> input)
        {
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
                return Utils.failure(400, "Credentials required.");

            LoginManager.CredentialLoginResult result =
                main.loginManager.LoginWithCredential(handler, username, password, domain);

            if (result.Success)
                return Utils.success("Your token has been created and stored.", new Dictionary<string, dynamic>
                {
                    { "token", result.Token.Token },
                    { "token_private", result.Token.Token_Priv }
                });

            if (result.Cause == LoginManager.LoginCause.BAD_CREDENTIAL) return Utils.failure(400, "Bad credential.");

            if (result.Cause == LoginManager.LoginCause.RATE_LIMIT_EXCEEDED)
            {
                return new Dictionary<string, dynamic>
                {
                    { "success", false },
                    { "code", 429 },
                    { "message", "Rate limit exceed." },
                    { "rate", result.CurrentRate }
                };
            }

            Debug.WriteLine("[Login] Unknown error: " + result.Cause);
            return Utils.failure(500, "Unknown error.");
        }
    }
}
