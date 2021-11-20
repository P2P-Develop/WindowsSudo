using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using WindowsSudo.Action;

namespace WindowsSudo
{
    public class TCPHandler
    {
        private TcpClient client;
        private ActionExecutor actions;
        private NetworkStream stream;
        private bool alive;

        public TCPHandler(TcpClient client, ActionExecutor actions)
        {
            this.client = client;
            this.actions = actions;
            stream = client.GetStream();
            stream.ReadTimeout = 5000;
            alive = true;
        }

        public void Handle()
        {
            try
            {
                while (alive)
                {
                    StringBuilder received_reqeust = new StringBuilder();
                    while (stream.DataAvailable && alive)
                    {
                        byte[] in_buffer = new byte[1024];
                        try
                        {
                            stream.Read(in_buffer, 0, in_buffer.Length);
                        }
                        catch (IOException)
                        {
                            Thread.Sleep(1);
                            if (!alive)
                                break;
                            continue;
                        }
                        char[] in_char = Encoding.ASCII.GetChars(in_buffer);

                        received_reqeust.Append(in_char);
                    }

                    if (received_reqeust.Length > 0)
                    {
                        string request = received_reqeust.ToString();
                        Dictionary<string, dynamic> response = HandleRequest(request);
                        byte[] out_buffer = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(response));
                        stream.Write(out_buffer, 0, out_buffer.Length);
                        stream.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }



        public void Send(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        public void Send(string str)
        {
            Send(Encoding.ASCII.GetBytes(str));
        }

        public Dictionary<string, dynamic> HandleRequest(string requestString)
        {
            Dictionary<string, dynamic> response = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> request = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(requestString);

            List<string> missingKeys;

            if ((missingKeys = Utils.checkArgs(new Dictionary<string, Type>()
            {
                {"action", typeof(string)},
            }, request)).Count > 0)
            {
                response["success"] = false;
                response["message"] = "Invalid or missing arguments: " + string.Join(", ", missingKeys);
                return response;
            }

            try
            {
                return actions.executeAction(request["action"], client, request);
            }
            catch (ActionNotFoundException)
            {
                response["success"] = false;
                response["code"] = 404;
                response["message"] = "Action not found";
                return response;
            }
            catch (Exception e)
            {
                response["success"] = false;
                response["code"] = 500;
                response["message"] = e.Message;
                return response;
            }
        }

        public void Shutdown()
        {
            alive = false;
            Send("{\"exit\": true}");
        }

    }
}
