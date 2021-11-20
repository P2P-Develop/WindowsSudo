namespace WindowsSudo
{
    internal static class Program
    {
        /// <summary>
        ///     アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        private static void Main()
        {
#if DEBUG
            Debug();
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new MainService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }

        private static void Debug()
        {
            MainService service = new MainService();
            service.TestStartupAndStop(new string[] { });
        }
    }
}
