using System;
using System.Collections.Generic;
using System.Timers;

namespace WindowsSudo
{
    public class RateLimiter
    {
        private readonly Timer _timer;

        public RateLimiter(RateLimitConfig rateLimitConfig)
        {
            this.rateLimitConfig = rateLimitConfig;
            attempts = new Dictionary<int, int>();
            throttles = new Dictionary<int, int>();

            Timer timer = new Timer(1000);
            timer.Elapsed += OnSecond;
            _timer = timer;
        }

        private RateLimitConfig rateLimitConfig { get; }
        private Dictionary<int, int> attempts { get; }
        private Dictionary<int, int> throttles { get; }

        public void Ready()
        {
            _timer.Start();
        }

        public bool OnAttemptLogin(TCPHandler tcpHandler)
        {
            var clientIdentifier = tcpHandler.Identify();

            var attempt = attempts.ContainsKey(clientIdentifier) ? attempts[clientIdentifier] : 0;

            attempts[clientIdentifier] = attempt + 1;

            if (!rateLimitConfig.SuppressActions.ContainsKey(attempt))
                return true;

            RateLimitConfig.SuppressAction suppressAction = rateLimitConfig.SuppressActions[attempt];

            DoPunish(tcpHandler, suppressAction);
            return false;
        }

        public void OnLoginSucceed(TCPHandler tcpHandler)
        {
            var clientIdentifier = tcpHandler.Identify();
            attempts.Remove(clientIdentifier);
            throttles.Remove(clientIdentifier);
        }

        public int GetCurrentRate(TCPHandler tcpHandler)
        {
            var clientIdentifier = tcpHandler.Identify();

            if (!attempts.ContainsKey(clientIdentifier))
                return 0;

            return attempts[clientIdentifier];
        }

        private void DoPunish(TCPHandler tcpHandler, RateLimitConfig.SuppressAction suppressAction)
        {
            switch (suppressAction)
            {
                case RateLimitConfig.SuppressAction.WARNING:
                    tcpHandler.Send(Utils.failure(429,
                        "You have made too many attempts to login. You have been warned."));
                    break;
                case RateLimitConfig.SuppressAction.KICK:
                    tcpHandler.Send(Utils.failure(429,
                        "You have made too many attempts to login. You have been kicked."));
                    tcpHandler.Shutdown();
                    break;
                default:
                    var seconds = (int)suppressAction;
                    if (seconds > 0)
                        SetThrottle(tcpHandler, 30);
                    break;
            }
        }

        private void SetThrottle(TCPHandler tcpHandler, int throttle)
        {
            throttles[tcpHandler.Identify()] = throttle;

            tcpHandler.Send(new Dictionary<string, dynamic>
            {
                { "success", false },
                { "message", "You have been throttled. Please try again in " + throttle + " seconds." },
                { "try_after", throttle }
            });
        }

        private void OnSecond(object sender, EventArgs e)
        {
            List<int> keys = new List<int>(throttles.Keys); // TODO: Verbose processing

            foreach (var clientIdentify in keys)
            {
                var throttle = throttles[clientIdentify];

                if (throttle > 0)
                    throttles[clientIdentify] = throttle - 1;
                else
                    throttles.Remove(clientIdentify);
            }
        }

        [Serializable]
        public class RateLimitConfig
        {
            public enum SuppressAction
            {
                WARNING = -1,
                KICK = -2,
                WAIT_FIVE_SECONDS = 5,
                WAIT_THIRTY_SECONDS = 30,
                WAIT_THREE_MINUTES = 3 * 60,
                WAIT_FIVE_MINUTES = 5 * 60
            }

            public RateLimitConfig(Dictionary<int, SuppressAction> suppressActions)
            {
                SuppressActions = suppressActions;
            }

            public RateLimitConfig()
            {
                Dictionary<int, SuppressAction> suppresses = new Dictionary<int, SuppressAction>();

                suppresses.Add(3, SuppressAction.WARNING);
                suppresses.Add(5, SuppressAction.KICK);
                suppresses.Add(6, SuppressAction.WAIT_FIVE_SECONDS);
                suppresses.Add(7, SuppressAction.WAIT_THIRTY_SECONDS);
                suppresses.Add(8, SuppressAction.WAIT_THIRTY_SECONDS);
                suppresses.Add(9, SuppressAction.WAIT_THREE_MINUTES);

                SuppressActions = suppresses;
            }

            public Dictionary<int, SuppressAction> SuppressActions { get; set; }
        }
    }
}
