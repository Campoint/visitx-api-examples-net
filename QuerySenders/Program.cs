using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Campoint.Visitx.API.Samples.Credentials;
using Newtonsoft.Json;

namespace Campoint.Visitx.API.Samples.QuerySenders
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var maleSenders = await QuerySenders(@"profile.gender == ""M""");
            Console.WriteLine($"There are {maleSenders.Count} male senders");

            var femaleSendersFromAschaffenburg =
                await QuerySenders(@"profile.gender == ""F"" && profile.city.Contains(""Aschaffenburg"")");
            Console.WriteLine($"There are {femaleSendersFromAschaffenburg.Count} female senders from Aschaffenburg");

            var femaleSendersWithShortHair = await QuerySenders(@"profile.gender == ""F"" && profile.hair_length1 == 1");
            Console.WriteLine($"There are {femaleSendersWithShortHair.Count} female senders with short hair");

            var slimOrAthleticFemales = await QuerySenders(@"profile.gender == ""F"" && (profile.figure1 == 1 || profile.figure1 == 2)");
            Console.WriteLine($"There are {slimOrAthleticFemales.Count} slim or athletic female senders");

            var twentyYearsAgo = DateTime.Now.AddYears(-20);
            var age20OrYounger = await QuerySenders(@"profile.birthday1 > new DateTime(" + twentyYearsAgo.Year + ", " + twentyYearsAgo.Month + ", " + twentyYearsAgo.Day + ")");

            Console.WriteLine($"There are {age20OrYounger.Count} senders younger than 20 years");
            var femaleSendersBelow50Kg = await QuerySenders(@"profile.weight1 < 50 && profile.weight1 > 0");
            
            Console.WriteLine($"There are {femaleSendersBelow50Kg.Count} female senders with a weight less than 50 kg");
            Console.ReadKey();
        }


        private static async Task<List<dynamic>> QuerySenders(string queryString)
        {
            var escapedQueryString = HttpUtility.UrlEncode(queryString);
        
            using (var client = new HttpClient())
            {
                var next = 0;
                const int chunkSize = 1000;
                var isDone = false;

                var senders = new List<dynamic>();
                do

                {
                   var sendersResponseContent = await client.GetStringAsync(
                        $"https://meta.visit-x.net/VXREST.svc/json/senders?skip={next}&take={chunkSize}&{ApiCredentials.AccessKeyQueryParam}&query={escapedQueryString}");
                    var currentSenders = JsonConvert.DeserializeObject<List<dynamic>>(sendersResponseContent) ?? new List<dynamic>();
                    senders.AddRange(currentSenders);

                    next += currentSenders.Count;
                    isDone = currentSenders.Count != chunkSize;
                } while (!isDone);

                return senders;
            }
        }
    }
}