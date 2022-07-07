using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace WindowsSudo
{
    public class RateLimiter
    {
        private RateLimitConfig rateLimitConfig { get; }
        private Dictionary<TCPHandler, int> attempts { get; }
        private Dictionary<TCPHandler, int> throttles { get; }
        
        private readonly Timer _timer;

        public RateLimiter(RateLimitConfig rateLimitConfig)
        {
            this.rateLimitConfig = rateLimitConfig;
            attempts = new Dictionary<TCPHandler, int>();
            throttles = new Dictionary<TCPHandler, int>();
            
            Timer timer = new Timer(1000);
            timer.Elapsed += OnSecond;
        }

        public void Ready()
        {
            _timer.Start();
        }
        
        public bool OnAttemptLogin(TCPHandler tcpHandler)
        {
            var attempt = attempts.ContainsKey(tcpHandler) ? attempts[tcpHandler] : 0;
            attempts[tcpHandler] = attempt;
            
            if (!rateLimitConfig.SuppressActions.ContainsKey(attempt))
                return true;

            RateLimitConfig.SuppressAction suppressAction = rateLimitConfig.SuppressActions[attempt];
            
            DoPunish(tcpHandler, suppressAction);
            return false;
        }
        
        private void DoPunish(TCPHandler tcpHandler, RateLimitConfig.SuppressAction suppressAction)
        {
            switch (suppressAction)
            {
                case RateLimitConfig.SuppressAction.WARNING:
                    tcpHandler.Send(Utils.failure(429, "You have made too many attempts to login. You have been warned."));
                    break;
                case RateLimitConfig.SuppressAction.WAIT_FIVE_SECONDS:
                    SetThrottle(tcpHandler, 5);
                    break;
                case RateLimitConfig.SuppressAction.WAIT_THIRTY_SECONDS:
                    SetThrottle(tcpHandler, 30);
                    break;
                case RateLimitConfig.SuppressAction.WAIT_THREE_MINUTES:
                    SetThrottle(tcpHandler, 3 * 60);
                    break;
                case RateLimitConfig.SuppressAction.KICK:
                    tcpHandler.Send(Utils.failure(429, "You have made too many attempts to login. You have been kicked."));
                    tcpHandler.Shutdown();
                    break;
                case RateLimitConfig.SuppressAction.BAN_TEMPORARILY:
                case RateLimitConfig.SuppressAction.BAN_PERMANENT:
                    tcpHandler.Send(Utils.failure(429, "You have made too many attempts to login. You have been temporarily banned."));
                    tcpHandler.Shutdown();
                    
                    // TODO: implement ban
                    break;
            }
        }

        private void SetThrottle(TCPHandler tcpHandler, int throttle)
        {
            throttles[tcpHandler] = throttle;
            
            tcpHandler.Send(new Dictionary<string, dynamic>()
            {
                {"success", false},
                {"message", "You have been throttled. Please try again in " + throttle + " seconds."},
                {"try_after", throttle}
            });
        }
        
        private void OnSecond(object sender, EventArgs e)
        {
            List<TCPHandler> keys = new List<TCPHandler>(attempts.Keys); // TODO: Verbose processing

            foreach (TCPHandler tcpHandler in keys)
            {
                var throttle = throttles[tcpHandler];
                
                if (throttle > 0)
                    throttles[tcpHandler] = throttle - 1;
                else
                    throttles.Remove(tcpHandler);
            }
        }
        
        [Serializable()]
        public class RateLimitConfig
        {
            public Dictionary<int, SuppressAction> SuppressActions { get; set; }

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
                suppresses.Add(10, SuppressAction.BAN_PERMANENT);
                
                
            }

            public enum SuppressAction
            {
                WARNING,
                KICK,
                WAIT_FIVE_SECONDS,
                WAIT_THIRTY_SECONDS,
                WAIT_THREE_MINUTES,
                BAN_TEMPORARILY,
                BAN_PERMANENT
            }
        }
    }
}
