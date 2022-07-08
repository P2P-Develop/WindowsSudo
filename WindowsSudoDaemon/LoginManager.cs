using System.Diagnostics;
using System.Management;

namespace WindowsSudo
{
    public class LoginManager
    {
        public enum LoginCause
        {
            None,
            BAD_CREDENTIAL,
            INVALID_TOKEN,
            RATE_LIMIT_EXCEEDED,
            INTERNAL_ERROR
        }

        private readonly RateLimiter rateLimiter;

        public LoginManager(RateLimiter.RateLimitConfig rateLimitConfig)
        {
            rateLimiter = new RateLimiter(rateLimitConfig);

#if DEBUG
            SelectQuery query = new SelectQuery("Win32_UserAccount");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

            Debug.WriteLine("[Login] Enumerating users...");
            foreach (ManagementObject envVar in searcher.Get())
                Debug.WriteLine("[Login] Username : {0}", envVar["Name"]);
#endif
        }

        public void Ready()
        {
            rateLimiter.Ready();
        }

        public CredentialLoginResult LoginWithCredential(TCPHandler client, string username, string password,
            string domain)
        {
            var rateOnFail = rateLimiter.GetCurrentRate(client) + 1;

            if (!rateLimiter.OnAttemptLogin(client))
            {
                Debug.WriteLine("[Login] A login has been blocked because the client has been rate limited.");
                return new CredentialLoginResult(false, null, LoginCause.RATE_LIMIT_EXCEEDED, rateOnFail);
            }

            if (!CheckCredential(domain, username, password))
            {
                Debug.WriteLine("[Login] Login failed for {0}@{1}", username, domain);
                rateLimiter.OnAttemptLogin(client);
                return new CredentialLoginResult(false, null, LoginCause.BAD_CREDENTIAL, rateOnFail);
            }

            Debug.WriteLine("[Login] Congratulations, they has passed all tests!");
            Debug.WriteLine("[Login] Login successful for {0}@{1}", username, domain);

            Debug.Write("[Login] Generating token...");
            TokenManager.TokenInfo token = TokenManager.GenerateToken(username, password, domain);
            Debug.WriteLine("DONE");
            client.token = token;

            return new CredentialLoginResult(true, token, LoginCause.None, 0);
        }

        public TokenLoginResult LoginWithToken(TCPHandler client, string token, string tokenPrivate)
        {
            var rateOnFail = rateLimiter.GetCurrentRate(client) + 1;

            if (!rateLimiter.OnAttemptLogin(client))
            {
                Debug.WriteLine("[Login] A login has been blocked because the client has been rate limited.");
                return new TokenLoginResult(false, LoginCause.RATE_LIMIT_EXCEEDED, rateOnFail);
            }

            Debug.Write("[Login] Validating token...");
            if (!TokenManager.ValidateToken(token, tokenPrivate))
            {
                Debug.WriteLine("[Login] Login failed with token.");
                rateLimiter.OnAttemptLogin(client);
                return new TokenLoginResult(false, LoginCause.INVALID_TOKEN, rateOnFail);
            }

            Debug.WriteLine("[Login] Login successful with token");

            client.token = TokenManager.GetTokenInfo(token);

            return new TokenLoginResult(true, LoginCause.None, 0);
        }

        private bool CheckCredential(string domain, string username, string password)
        {
            Debug.WriteLine("[Login] Checking provided credential is valid...");

            try
            {
                return CredentialHelper.ValidateAccount(username, password, domain, true);
            }
            catch (CredentialHelper.Exceptions.BadPasswordException)
            {
                Debug.WriteLine("[Login] Domain exists(or not specified), user exists, but password is wrong. hmm...");
                return false;
            }
            catch (CredentialHelper.Exceptions.DomainNotFoundException)
            {
                Debug.WriteLine("[Login] Domain does not exist.");
                return false;
            }
            catch (CredentialHelper.Exceptions.PasswordRequiredException)
            {
                Debug.WriteLine("[Login] Password is required, but not provided.");
                return false;
            }
            catch (CredentialHelper.Exceptions.UserNotFoundException)
            {
                Debug.WriteLine("[Login] User does not exist.");
                return false;
            }
        }

        public class CredentialLoginResult
        {
            public CredentialLoginResult(bool success, TokenManager.TokenInfo token, LoginCause cause, int currentRate)
            {
                Success = success;
                Token = token;
                Cause = cause;
                CurrentRate = currentRate;
            }

            public bool Success { get; }
            public TokenManager.TokenInfo Token { get; }

            public LoginCause Cause { get; }

            public int CurrentRate { get; }
        }

        public class TokenLoginResult
        {
            public TokenLoginResult(bool success, LoginCause cause, int currentRate)
            {
                Success = success;
                Cause = cause;
                CurrentRate = currentRate;
            }

            public bool Success { get; }

            public LoginCause Cause { get; }

            public int CurrentRate { get; }
        }
    }
}
