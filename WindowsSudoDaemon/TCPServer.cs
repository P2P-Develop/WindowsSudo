using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WindowsSudo.Action;

namespace WindowsSudo
{
    public class TCPServer
    {
        private readonly ActionExecutor actions;
        private readonly List<TCPHandler> handlers;
        private readonly string host;
        private readonly TcpListener listener;
        private readonly int port;

        public bool alive;

        public TCPServer(string host, int port, ActionExecutor actions)
        {
            this.host = host;
            this.port = port;
            this.actions = actions;
            handlers = new List<TCPHandler>();
            listener = new TcpListener(IPAddress.Parse(host), port);
            alive = true;
        }

        public void Start()
        {
            Debug.WriteLine("[Server] Starting...");
            listener.Start();

            Debug.WriteLine("[Server] Listening on {0}:{1}", host, port);
            while (alive)
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Debug.WriteLine("[Server] <== [{0}] Client connection established: ", client.Client.RemoteEndPoint);

                    TCPHandler handler = new TCPHandler(client, actions);
                    handlers.Add(handler);
                    Thread clientThread = new Thread(() =>
                    {
                        handler.Handle();
                        handlers.Remove(handler);
                    });

                    clientThread.Start();
                }
                catch (SocketException e)
                {
                    if (!alive)
                        return;
                    Debug.WriteLine("[Server] </= [{0}] An error has occured: {1}", host, e.Message);
                    Debug.WriteLine(e.ToString());
                }
        }

        public void Stop()
        {
            Debug.WriteLine("[Server] Stopping...");

            alive = false;

            foreach (TCPHandler handler in handlers)
                handler.Shutdown();
            Thread.Sleep(500);
            listener.Stop();
        }
    }
}
