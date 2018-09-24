using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Campoint.Visitx.API.Samples.FetchSenders
{
    class Program
    {
        public static string AccessKey = "";

        public static async Task Main(string[] args)
        {
            var accessKeyQueryParam = "accessKey=" + AccessKey;
         
            using (var client = new HttpClient())
            {
                var senders = await FetchSenders(client, accessKeyQueryParam);
                Console.WriteLine("Fetched {0} senders", senders.Count);

                var senderId = (int)senders.First().UserID;
                var senderSedCards = await GetSenderSedCards(client, senderId, accessKeyQueryParam);
                var picturesInSedCards = senderSedCards.SelectMany<dynamic, dynamic>(sedCard => sedCard.Pictures).ToList();
                
                Console.Write($"Sender {senderId} has {senderSedCards.Count()} sed cards with {picturesInSedCards.Count()} pictures in total.");

                Console.ReadKey();
            }
        }

        private static async Task<IList<dynamic>> GetSenderSedCards(HttpClient client, int senderId, string accessKeyQueryParam)
        {
            var sedCardsResponse = await client.GetAsync($"https://meta.visit-x.net/VXREST.svc/json/senders/{senderId}/sedcards?{accessKeyQueryParam}");
            var sedCardsContent = await sedCardsResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject < List <dynamic >> (sedCardsContent);
        }

        private static async Task<List<dynamic>> FetchSenders(HttpClient client, string accessKeyQueryParam)
        {
            var next = 0;
            const int chunkSize = 1000;
            var isDone = false;
            var senders = new List<dynamic>();

            do
            {
                var sendersResponse = await client.GetAsync("https://meta.visit-x.net/VXREST.svc/json/senders?skip=" + next +
                                                            " &take= " + chunkSize + "&" + accessKeyQueryParam);
                var sendersResponseContent = await sendersResponse.Content.ReadAsStringAsync();

                var currentSenders = JsonConvert.DeserializeObject<List<dynamic>>(sendersResponseContent);
                senders.AddRange(currentSenders);

                next += currentSenders.Count;
                isDone = currentSenders.Count != chunkSize;
            } while (!isDone);

            return senders;
        }
    }
}
