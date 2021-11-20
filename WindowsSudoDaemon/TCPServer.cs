using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            listener.Start();
            Debug.WriteLine("Listening on {0}:{1}", host, port);
            while (alive)
            {

                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Debug.WriteLine("Client connected");
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
                    Debug.WriteLine("SocketException: {0}", e);
                }
            }
        }

        public void Stop()
        {
            alive = false;


            foreach (TCPHandler handler in handlers)
                handler.Shutdown();
            Thread.Sleep(500);
            listener.Stop();
        }

    }
}
