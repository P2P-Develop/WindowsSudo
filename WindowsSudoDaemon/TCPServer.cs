using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using WindowsSudo.Action;

namespace WindowsSudo
{
    public class TCPServer
    {
        private readonly string host;
        private readonly int port;
        private readonly ActionExecutor actions;
        private readonly TcpListener listener;
        private readonly List<TCPHandler> handlers;

        private bool alaive;

        public TCPServer(string host, int port, ActionExecutor actions)
        {
            this.host = host;
            this.port = port;
            this.actions = actions;
            handlers = new List<TCPHandler>();
            listener = new TcpListener(IPAddress.Parse(host), port);
            alaive = true;
        }

        public void Start()
        {
            listener.Start();
            Console.WriteLine("Listening on {0}:{1}", host, port);
            while (alaive)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected");
                TCPHandler handler = new TCPHandler(client, actions);
                handlers.Add(handler);
                Thread clientThread = null;
                clientThread = new Thread(() =>
                {
                    handler.Handle();
                    handlers.Remove(handler);
                });

                clientThread.Start();
            }
        }

        public void Stop()
        {
            alaive = false;

            foreach (TCPHandler handler in handlers)
                handler.Shutdown();
            Thread.Sleep(6500);
            listener.Stop();
        }

    }
}
