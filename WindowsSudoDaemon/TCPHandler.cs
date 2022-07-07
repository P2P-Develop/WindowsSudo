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
        private readonly ActionExecutor actions;
        private readonly TcpClient client;
        private readonly NetworkStream stream;
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
                        var in_buffer = new byte[1024];
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

                        var in_char = Encoding.UTF8.GetChars(in_buffer);

                        received_reqeust.Append(in_char);
                    }

                    try
                    {
                        if (received_reqeust.Length > 0)
                        {
                            var request = received_reqeust.ToString();
                            Dictionary<string, dynamic> response = HandleRequest(request);
                            Send(response);
                        }
                    }
                    catch(JsonReaderException e)
                    {
                        Debug.WriteLine("[Server] </~ [{0}] Request handle failed: Failed to parse json.", client.Client.RemoteEndPoint);
                        Send(JsonConvert.SerializeObject(new Dictionary<string, dynamic>
                        {
                            {"success", false},
                            {"code", 400},
                            {"message", "Failed to parse request."},
                        }));
                    }
                    finally
                    {
                        received_reqeust.Clear();
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
            Send(Encoding.UTF8.GetBytes(str));
        }
        
        public void Send(dynamic response)
        {
            Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
        }

        public Dictionary<string, dynamic> HandleRequest(string requestString)
        {
            Dictionary<string, dynamic> response = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> request =
                JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(requestString);

            List<string> missingKeys;

            if ((missingKeys = Utils.checkArgs(new Dictionary<string, Type>
            {
                { "action", typeof(string) }
            }, request)).Count > 0)
            {
                Debug.WriteLine("[Server] </~ [{0}] Request handle failed: Parameter 'action' not found.", client.Client.RemoteEndPoint);
                response["success"] = false;
                response["message"] = "Invalid or missing arguments: " + string.Join(", ", missingKeys);
                return response;
            }
            
            Debug.WriteLine("[Server] <~~ [{0}] Received Request", client.Client.RemoteEndPoint);

            try
            {
                return actions.executeAction(request["action"], client, request);
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine("[Server] </~ [{0}] Missing argument: {1}", client.Client.RemoteEndPoint, e.Message);
                response["success"] = false;
                response["code"] = 400;
                response["message"] = "Invalid or missing arguments";
                return response;
            }
            catch (ActionNotFoundException)
            {
                Debug.WriteLine("[Server] </~ [{0}] Action not found: {1}", client.Client.RemoteEndPoint, request["action"]);
                response["success"] = false;
                response["code"] = 404;
                response["message"] = "Action not found";
                return response;
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Server] </~ [{0}] An exception has occurred: ", client.Client.RemoteEndPoint);
                Debug.WriteLine(e);
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
            client.Close();
        }
    }
}
