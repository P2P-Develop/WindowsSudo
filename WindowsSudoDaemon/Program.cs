using System.Diagnostics;

namespace WindowsSudo
{
    internal static class Program
    {
        /// <summary>
        ///     アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        private static void Main()
        {
            Debug.WriteLine("Entrypoint has called.");
#if DEBUG
            RunDebug();
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new MainService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }

        private static void RunDebug()
        {
            Debug.WriteLine("Debug mode is enabled.");
            Debug.WriteLine("Launch...");
            MainService service = new MainService();
            service.TestStartupAndStop(new string[] { });
        }
    }
}
