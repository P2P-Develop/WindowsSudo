using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Timers;

namespace WindowsSudo
{
    public class TokenManager
    {
        private static TokenManager _instance;
        private readonly object _lock = new object();

        private readonly Dictionary<string, TokenInfo> _tokens;
        private readonly Timer _timer;
        private readonly SHA256CryptoServiceProvider _sha256;


        private TokenManager()
        {
            _tokens = new Dictionary<string, TokenInfo>();
            _sha256 = new SHA256CryptoServiceProvider();
            _timer = new Timer(1000);
            _timer.Elapsed += OnTimer;
        }

        public static void Ready()
        {
            if (_instance == null)
            {
                lock (typeof(TokenManager))
                {
                    if (_instance == null)
                    {
                        _instance = new TokenManager();
                    }
                }
            }

            lock (_instance._lock)
            {
                _instance._timer.Start();
            }
        }

        public static TokenInfo GenerateToken(string username, string password, string domain)
        {
            CredentialHelper.ValidateAccount(username, password, domain, true);

            TokenInfo token = new TokenInfo(username, password, domain);

            lock(_instance._lock)
            {
                _instance._tokens.Add(token.Token, token);
            }

            return token;
        }



        private void OnTimer(object sender, EventArgs e)
        {
            lock (_instance._lock)
            {
                foreach (TokenInfo tokenInfo in _tokens.Values)
                {
                    int duration = tokenInfo.Duration -= 1;
                    if (duration <= 0)
                    {
                        _tokens.Remove(tokenInfo.Username);
                        Debug.WriteLine("Token expired: " + tokenInfo.Username);
                    }
                }
            }
        }

        public static bool ValidateToken(string username, string token, string token_priv)
        {
            lock(_instance._lock)
            {
                if (_instance._tokens.ContainsKey(token))
                {
                    TokenInfo tokenInfo = _instance._tokens[token];
                    return tokenInfo.Token == token && tokenInfo.Token_Priv == token_priv &&
                           tokenInfo.Username == username;
                }
                else
                    return false;
            }
        }

        public static TokenInfo GetTokenInfo(string token)
        {
            lock (_instance._lock)
            {
                if (_instance._tokens.ContainsKey(token))
                {
                    TokenInfo tokenInfo = _instance._tokens[token];
                    return tokenInfo;
                }
                else
                    return null;
            }
        }

        public class TokenInfo
        {
            public string Username { get; }
            public SecureString Password { get; }
            public string Domain { get; }
            public int Duration { get; set; }
            public string Token { get; }
            public string Token_Priv { get; }

            public TokenInfo(string username, string password, string domain)
            {
                Username = username;
                Password = new NetworkCredential("", password).SecurePassword;
                Domain = domain;
                Duration = 15 * 60;
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                Token_Priv = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                        string.Join("", _instance._sha256.ComputeHash(
                            Encoding.UTF8.GetBytes(username + password + domain + DateTime.Now
                            )).Select(x => $"{x:x2}"));
            }
        }
    }
}
