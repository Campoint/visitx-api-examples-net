using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Campoint.Visitx.API.Samples.Credentials;
using Newtonsoft.Json;

namespace Campoint.Visitx.API.Samples.TranslateProfile
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var client = new HttpClient())
            {
                var sender = await FetchFirstSender(client);

                var senderProfile = await GetSenderProfile(client, sender);

                const string languageCode = "DE";

                foreach (var property in senderProfile.ProfileProperties)
                {
                    if(property.LanguageCode == null || property.LanguageCode == languageCode)
                    { 
                        var propertyName = property.Key;
                        var propertyValue = await GetPropertyValue(property, client, languageCode);

                        Console.WriteLine($"{propertyName} : {propertyValue}");
                    }
                }
                
                Console.ReadKey();
            }
        }

        private static async Task<string> GetPropertyValue(dynamic property, HttpClient client, string languageCode)
        {
            var value = property.Value.Value;

            if (property.Value.LanguageCode == null && property.Value.TranslationKey != null)
            {
                var translationKey = (string) property.Value.TranslationKey;

                var valueTranslations = await GetTranslations(client, languageCode, translationKey);

                if (IsMultiValue(property.Value))
                {
                    var propertyValues = ((string)value).Split(',');

                    return string.Join(", ", propertyValues.Select(v => TranslateValue(v, valueTranslations)));
                }
                else
                {
                    return TranslateValue(value, valueTranslations);
                }
            }

            return value;
        }

        private static bool IsMultiValue(dynamic value)
        {
            return ((string) value.__type).StartsWith("MultiValueProfileProperty");
        }

        private static string TranslateValue(dynamic value, Dictionary<string, string> valueTranslations)
        {
            var valueTranslationKey = value.ToString();

            if (valueTranslations.TryGetValue(valueTranslationKey, out string translation))
            {
                return translation;
            }
            else
            {
                return value;
            }
        }
        
        private static async Task<Dictionary<string, string>> GetTranslations(HttpClient client, string languageCode, string translationKey)
        {
            var translationsResponse = await client.GetAsync($"https://meta.visit-x.net/VXREST.svc/json/translations/{translationKey}/{languageCode}?{ApiCredentials.AccessKeyQueryParam}");
            var translationsContent = await translationsResponse.Content.ReadAsStringAsync();
            var translations = JsonConvert.DeserializeObject<List<dynamic>>(translationsContent);
            return translations.ToDictionary(j => (string)j.Index, j => (string)j.Value);
        }

        private static async Task<dynamic> GetSenderProfile(HttpClient client, dynamic sender)
        {
            var senderId = sender.UserID;

            var profileResponse = await client.GetAsync($"https://meta.visit-x.net/VXREST.svc/json/senders/{senderId}/profile?{ApiCredentials.AccessKeyQueryParam}");
            var profileContent = await profileResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<dynamic>(profileContent);
        }

        private static async Task<dynamic> FetchFirstSender(HttpClient client)
        {
            var sendersResponse = await client.GetAsync("https://meta.visit-x.net/VXREST.svc/json/senders?skip=0&take=1&" + ApiCredentials.AccessKeyQueryParam);
            var sendersResponseContent = await sendersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<dynamic>>(sendersResponseContent).Single();
        }
    }
}
