using System;
using System.Dynamic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Campoint.Visitx.API.Samples.GetOnlineStates
{
    class Program
    {
        public static string AccessKey = "";

        public static async Task Main(string[] args)
        {
            var webSocketUri = new Uri($"wss://data.campoints.net/?accessKey={AccessKey}");

            using (var clientWs = new ClientWebSocket())
            { 
                // connect the client to the websocket
                await clientWs.ConnectAsync(webSocketUri, CancellationToken.None);

                var buffer = new ArraySegment<byte>(new byte[64 * 1024]);
                var resultBuilder = new StringBuilder();

                // receive data as long as the websocket is open
                while (clientWs.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    resultBuilder.Clear();

                    // as the messages are chunked, fetch data and store it into a string builder until the end
                    // of the message is reached
                    do
                    {

                        result = await clientWs.ReceiveAsync(buffer, CancellationToken.None);
                        resultBuilder.Append(Encoding.UTF8.GetString(buffer.Array, 0, result.Count));
                    } while (!result.EndOfMessage);

                    var msg = resultBuilder.ToString();

                    if (!string.IsNullOrWhiteSpace(msg)) // Sometimes the server sends empty messages to keep the connection alive, ignore them
                    { 
                        dynamic msgObject = JsonConvert.DeserializeObject<ExpandoObject>(msg);
                      

                        // check if it is a online state changed message
                        if (msgObject.type == "vx.onlineState.videoChat" && msgObject.deleted == false)
                        {
                            var senderId = msgObject.data.user._ref.key;

                            Console.WriteLine(IsAvailableForChat(msgObject)
                                ? $"Sender {senderId} is now available for chat."
                                : $"Sender {senderId} is no longer available for chat.");
                        }
                    }
                }
            }
        }

        private static bool IsAvailableForChat(dynamic msgObject)
        {
            if (msgObject.deleted == true)
            {
                return false;
            }

            var msgPayload = msgObject.data;
            return msgPayload.voyeur == true
                   || msgPayload.multi == true 
                   || msgPayload.single == true 
                   || msgPayload.messenger == true;
        }
    }
}
