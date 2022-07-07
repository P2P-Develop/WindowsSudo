﻿using System;
using System.Collections.Generic;
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

        public string basePath;
        public FileConfiguration config;
        public ActionExecutor actions;
        public ProcessManager processManager;
        public TCPServer server;
        public Thread serverThread;
        public RateLimiter rateLimiter;

        public MainService()
        {
            InitializeComponent();

            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WindowsSudo");
            config = new FileConfiguration(Path.Combine(basePath, "config.json"));
            saveDefaultConfig(config);
            
            actions = new ActionExecutor(this);
            server = new TCPServer(config.GetString("network.host"), config.GetInteger("network.port"), actions);
            serverThread = new Thread(() => server.Start());
            processManager = new ProcessManager(this);
            rateLimiter = new RateLimiter(new RateLimiter.RateLimitConfig()); // TODO: load config
            
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
                        {"host", "127.0.0.1"},
                        {"port", 14105}
                    }}
            });
        }
    }
}
