using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Campoint.Visitx.API.Samples.Credentials;
using Newtonsoft.Json;

namespace Campoint.Visitx.API.Samples.FetchSenders
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            using (var client = new HttpClient())
            {
                var senders = await FetchSenders(client);
                Console.WriteLine("Fetched {0} senders", senders.Count);

                var senderId = (int)senders.First().UserID;
                var senderSedCards = await GetSenderSedCards(client, senderId);
                var picturesInSedCards = senderSedCards.SelectMany<dynamic, dynamic>(sedCard => sedCard.Pictures).ToList();
                
                Console.Write($"Sender {senderId} has {senderSedCards.Count()} sed cards with {picturesInSedCards.Count()} pictures in total.");

                Console.ReadKey();
            }
        }

        private static async Task<IList<dynamic>> GetSenderSedCards(HttpClient client, int senderId)
        {
            var sedCardsResponse = await client.GetAsync($"https://meta.visit-x.net/VXREST.svc/json/senders/{senderId}/sedcards?{ApiCredentials.AccessKeyQueryParam}");
            var sedCardsContent = await sedCardsResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject < List <dynamic >> (sedCardsContent);
        }

        private static async Task<List<dynamic>> FetchSenders(HttpClient client)
        {
            var next = 0;
            const int chunkSize = 1000;
            var isDone = false;
            var senders = new List<dynamic>();

            do
            {
                var sendersResponse = await client.GetAsync("https://meta.visit-x.net/VXREST.svc/json/senders?skip=" + next +
                                                            " &take= " + chunkSize + "&" + ApiCredentials.AccessKeyQueryParam);
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
