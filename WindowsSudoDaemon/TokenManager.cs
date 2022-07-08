using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly SHA256CryptoServiceProvider _sha256;
        private readonly Timer _timer;

        private readonly Dictionary<string, TokenInfo> _tokens;


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
                lock (typeof(TokenManager))
                {
                    if (_instance == null)
                        _instance = new TokenManager();
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

            lock (_instance._lock)
            {
                _instance._tokens.Add(token.Token, token);
            }

            return token;
        }

        public static void InvalidateToken(string token)
        {
            lock (_instance._lock)
            {
                _instance._tokens.Remove(token);
            }
        }

        private void OnTimer(object sender, EventArgs e)
        {
            lock (_instance._lock)
            {
                foreach (TokenInfo tokenInfo in _tokens.Values)
                {
                    var duration = tokenInfo.Duration -= 1;
                    if (duration <= 0)
                    {
                        _tokens.Remove(tokenInfo.Token);
                        Debug.WriteLine("Token expired: " + tokenInfo.Username);
                    }
                }
            }
        }

        public static bool ValidateToken(string token, string token_priv)
        {
            lock (_instance._lock)
            {
                if (_instance._tokens.ContainsKey(token))
                {
                    TokenInfo tokenInfo = _instance._tokens[token];
                    return tokenInfo.Token == token && tokenInfo.Token_Priv == token_priv;
                }

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

                return null;
            }
        }

        public class TokenInfo
        {
            public TokenInfo(string username, string password, string domain)
            {
                Username = username;
                Password = new NetworkCredential("", password).SecurePassword;
                Domain = domain;
                Duration = 15 * 60;
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                Token_Priv = Convert.ToBase64String(_instance._sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(username + domain + DateTime.Now)));
            }

            public string Username { get; }
            public SecureString Password { get; }
            public string Domain { get; }
            public int Duration { get; set; }
            public string Token { get; }
            public string Token_Priv { get; }

            public bool IsAlive()
            {
                return Duration > 0;
            }
        }
    }
}
