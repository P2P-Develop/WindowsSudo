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
        public ProcessManager processManager;
        public TCPServer server;
        public Thread serverThread;

        public MainService()
        {
            InitializeComponent();
            actions = new ActionExecutor(this);
            server = new TCPServer("127.0.0.1", 14105, actions);
            serverThread = new Thread(() => server.Start());
            processManager = new ProcessManager(this);

            registerActions();

            Instance = this;
        }


        private void registerActions()
        {
            actions.registerAction(new Exit());
            actions.registerAction(new Info());
            actions.registerAction(new Sudo());
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
    }
}
