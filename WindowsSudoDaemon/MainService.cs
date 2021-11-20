using System;
using System.ServiceProcess;
using WindowsSudo.Action;
using WindowsSudo.Action.Actions;

namespace WindowsSudo
{
    public partial class MainService : ServiceBase
    {
        public static MainService Instance;


        public ActionExecutor actions;
        public TCPServer server;

        public MainService()
        {
            InitializeComponent();
            actions = new ActionExecutor(this);
            server = new TCPServer("127.0.0.1", 14105, actions);

            registerActions();

            Instance = this;

        }


        private void registerActions()
        {
            actions.registerAction(new Exit());
        }

        protected override void OnStart(string[] args)
        {
            server.Start();
        }

        protected override void OnStop()
        {
            server.Stop();
        }

        public void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }
    }
}
