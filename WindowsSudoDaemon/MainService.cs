using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using WindowsSudo.Action;
using WindowsSudo.Action.Actions;

namespace WindowsSudo
{
    public partial class MainService : ServiceBase
    {
        public static MainService Instance;
        public ActionExecutor actions;

        public string basePath;
        public FileConfiguration config;
        public ProcessManager processManager;
        public RateLimiter rateLimiter;
        public TCPServer server;
        public Thread serverThread;

        public MainService()
        {
            Debug.WriteLine("MainService constructor called.");
            InitializeComponent();

            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "WindowsSudo");
            config = new FileConfiguration(Path.Combine(basePath, "config.json"));

            Debug.WriteLine("Saving default config...");
            saveDefaultConfig(config);

            actions = new ActionExecutor(this);
            server = new TCPServer(config.GetString("network.host"), config.GetInteger("network.port"), actions);
            serverThread = new Thread(() => server.Start());
            processManager = new ProcessManager(this);
            rateLimiter = new RateLimiter(new RateLimiter.RateLimitConfig()); // TODO: load config

            rateLimiter.Ready();
            TokenManager.Ready();

            registerActions();

            Instance = this;
        }

        private void registerActions()
        {
            actions.registerAction(new Exit());
            actions.registerAction(new Info());
            actions.registerAction(new Sudo());
            actions.registerAction(new GenToken());
        }

        protected override void OnStart(string[] args)
        {
            serverThread.Start();
        }

        protected override void OnStop()
        {
            server.Stop();
        }

        public void TestStartupAndStop(string[] args)
        {
            OnStart(args);
            while (server.alive)
                Thread.Sleep(100);
        }

        private static void saveDefaultConfig(FileConfiguration fileConfiguration)
        {
            fileConfiguration.SaveDefaultConfig(new Dictionary<string, dynamic>
            {
                {
                    "network", new Dictionary<string, dynamic>
                    {
                        { "host", "127.0.0.1" },
                        { "port", 14105 }
                    }
                }
            });
        }
    }
}
