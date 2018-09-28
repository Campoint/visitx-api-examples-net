using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Campoint.Visitx.API.Samples.Credentials;
using Newtonsoft.Json;

namespace Campoint.Visitx.API.Samples.BuyContent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var client = new HttpClient())
            {
                // set the authorization header here instead of using alternative methods as they might lead to suboptimal behavior
                // see https://stackoverflow.com/questions/25761214/why-would-my-rest-service-net-clients-send-every-request-without-authentication
                var authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ApiCredentials.UserName}:{ApiCredentials.Password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authentication);

                var demoSenders = await FetchDemoSenders(client);

                var demoSenderWithShopGallery = await Task.WhenAll(demoSenders.Select(async ds => new
                {
                    Sender = ds,
                    ShoppingGalleries = (List<dynamic>)(await GetShopGalleries(client, ds))
                }));

                var demoSenderWithExistingGalleries = demoSenderWithShopGallery.First(dssg => dssg.ShoppingGalleries.Count(HasValidGalleryPrice) != 0);

                var galleryToBuy = demoSenderWithExistingGalleries.ShoppingGalleries.First(HasValidGalleryPrice);

                await BuyGallery(client, galleryToBuy);
               

  
            }
        }

        private static bool HasValidGalleryPrice(dynamic g)
        {
            return g.Price > 0;
        }

        private static async Task BuyGallery(HttpClient client, dynamic galleryToBuy)
        {
            var cid = galleryToBuy.UmaId;

            var buyResponse = await client.GetStringAsync($"https://visit-x.net/interfaces/content/buy.php?cid={cid}&uip=10.10.10.10&type=G");
            var buyId = XDocument.Parse(buyResponse).Root.Value.Trim();

            var getLinksResponse =
                await client.GetStringAsync($"https://visit-x.net/interfaces/content/getLinks.php?bid={buyId}");
            var links = XDocument.Parse(getLinksResponse);
            Console.WriteLine(links);
        }
        

        private static async Task<List<dynamic>> GetShopGalleries(HttpClient client, dynamic sender)
        {
            var senderId = sender.UserID;
            
            var response = await client.GetAsync(
                $"https://meta.visit-x.net/VXREST.svc/json/senders/{senderId}/shopgalleries?{ApiCredentials.AccessKeyQueryParam}");

            return response.StatusCode == HttpStatusCode.NotFound ? new List<dynamic>() : JsonConvert.DeserializeObject<List <dynamic>>(await response.Content.ReadAsStringAsync());
        }
        
        private static async Task<List<dynamic>> FetchDemoSenders(HttpClient client)
        {
            var next = 0;
            const int chunkSize = 1000;
            var isDone = false;
            var senders = new List<dynamic>();

            do
            {
                var queryForTestAccounts = HttpUtility.UrlEncode("sender.IsTestLogin == true");
                var sendersResponseContent = await client.GetStringAsync($"https://meta.visit-x.net/VXREST.svc/json/senders?skip={next}&take={chunkSize}&{ApiCredentials.AccessKeyQueryParam}&query={queryForTestAccounts}");
                var currentSenders = JsonConvert.DeserializeObject<List<dynamic>>(sendersResponseContent);
                senders.AddRange(currentSenders);

                next += currentSenders.Count;
                isDone = currentSenders.Count != chunkSize;
            } while (!isDone);

            return senders;
        }
    }
}
