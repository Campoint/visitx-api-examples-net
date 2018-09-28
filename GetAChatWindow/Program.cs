using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Campoint.Visitx.API.Samples.Credentials;
using Newtonsoft.Json;

namespace Campoint.Visitx.API.Samples.GetAChatWindow
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // return the first sender that gets onine after the program has started
            var availableChatPartner = await GetAvailableChatPartner();

            // open a chat window with this sender
            var chatWindow = await InitiateChat(availableChatPartner);

            Console.WriteLine("Chat Window Element:");
            Console.WriteLine(chatWindow);

            Console.ReadKey();
        }

        private static async Task<dynamic> GetAvailableChatPartner()
        {
            var webSocketUri = new Uri($"wss://data.campoints.net/?{ApiCredentials.AccessKeyQueryParam}");

            // connect to the websocket
            using (var ws = await ConnectWebSocket(webSocketUri, CancellationToken.None))
            {
                // create a observable to watch for messages received
                var o = Observable.FromAsync((cancel) => ReceiveMessage(ws, cancel)).Repeat();
                
                // filter for online status update messages
                var updateMessages = o.Select(ToJsonObject).Where(d => d != null).Where(IsNotADeleteMessage).Where(IsOnlineUpdateMessage);

                // return the first sender going available for chat
                return await updateMessages.Where(IsAvailableForChat).FirstAsync();
            }
        }

        private static bool IsOnlineUpdateMessage(dynamic msg)
        {
            return msg.type == "vx.onlineState.videoChat";
        }

        private static async Task<string> InitiateChat(dynamic availableChatPartner)
        {
            // get the sender's id
            var senderId = availableChatPartner.data.user._ref.key;
            
            using (var httpClient = new HttpClient())
            {
                // set the authorization header here instead of using alternative methods as they might lead to suboptimal behavior
                // see https://stackoverflow.com/questions/25761214/why-would-my-rest-service-net-clients-send-every-request-without-authentication
                var authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ApiCredentials.UserName}:{ApiCredentials.Password}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authentication);

                var uri = $"https://visit-x.net/interfaces/content/start.php?userID={senderId}";
                var sendersResponse = await httpClient.GetAsync(uri);

                return await sendersResponse.Content.ReadAsStringAsync();
            }
        }


        private static bool IsAvailableForChat(dynamic jsonMessage)
        {
            var data = jsonMessage.data;
            return data.voyeur == true
                   || data.multi == true
                   || data.single == true
                   || data.messenger == true;
        }

        private static bool IsNotADeleteMessage(dynamic jsonMessage)
        {
            return jsonMessage.deleted == false;
        }

        private static dynamic ToJsonObject(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return (dynamic)null;
            }

            return JsonConvert.DeserializeObject<dynamic>(message);
        }

        private static async Task<ClientWebSocket> ConnectWebSocket(Uri uri, CancellationToken cancel)
        {
            var ws = new ClientWebSocket();
            await ws.ConnectAsync(uri, cancel);
            return ws;
        }

        private static async Task<string> ReceiveMessage(WebSocket clientWs, CancellationToken cancel)
        {
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
                    result = await clientWs.ReceiveAsync(buffer, cancel);
                    resultBuilder.Append(Encoding.UTF8.GetString(buffer.Array, 0, result.Count));
                } while (!result.EndOfMessage && !cancel.IsCancellationRequested);

                return resultBuilder.ToString();
            }

            return null;
        }

    }
}
