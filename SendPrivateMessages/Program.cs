using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Campoint.Visitx.API.Samples.Credentials;
using Newtonsoft.Json;

namespace Campoint.Visitx.API.Samples.SendPrivateMessages
{
    class Program
    {
        static async Task Main(string[] args) {


            using (var client = new HttpClient())
            {
                // set the authorization header here instead of using alternative methods as they might lead to suboptimal behavior
                // see https://stackoverflow.com/questions/25761214/why-would-my-rest-service-net-clients-send-every-request-without-authentication
                var authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ApiCredentials.UserName}:{ApiCredentials.Password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authentication);

                var testAccountId = (string)(await FetchDemoSender(client)).UserID;

                await SendPrivateMessages(client, testAccountId);
                
                Console.ReadKey();
            }

        }

        private static async Task SendPrivateMessages(HttpClient client, string testAccountId)
        {
            HttpContent messageContent = new FormUrlEncodedContent
                (new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("guestId", ApiCredentials.UserName.GetHashCode().ToString()),
                new KeyValuePair<string, string>("guest", ApiCredentials.UserName),
                new KeyValuePair<string, string>("host", testAccountId),
                new KeyValuePair<string, string>("text", ".NET API Sample Message"),
            });

            var response = await client.PostAsync("https://www.visit-x.net/smif/contentpartner/sendmail", messageContent);
            var messageId = await response.Content.ReadAsStringAsync();
            Console.Write($"Successfully send a private message with id {messageId} to sender {testAccountId}");
        }

        private static async Task<dynamic> FetchDemoSender(HttpClient client)
        {
            var next = 0;
            const int chunkSize = 1;
            
            var queryForTestAccounts = HttpUtility.UrlEncode(@"sender.IsTestLogin == true && sender.Sendername == ""Froschhueter""");
            var sendersResponseContent = await client.GetStringAsync($"https://meta.visit-x.net/VXREST.svc/json/senders?skip={next}&take={chunkSize}&{ApiCredentials.AccessKeyQueryParam}&query={queryForTestAccounts}");
            var senders = JsonConvert.DeserializeObject<List<dynamic>>(sendersResponseContent);
            return senders.Single();
           
        }
    }
}
