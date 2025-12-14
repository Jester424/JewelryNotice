using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using Newtonsoft.Json;
using Microsoft.Toolkit.Uwp.Notifications;


namespace JewelryNotice
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("JewelryNoticeKey");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API key not found. Set JewelryNoticeKey environmental variable.");
                return;
            }

            bool securityOffline = await CheckSecurity(apiKey);

            Console.WriteLine(securityOffline.ToString());
        }

        private static async Task<bool> CheckSecurity(string apiKey)
        {
            string url = "https://api.torn.com/torn/?selections=shoplifting&key=" + apiKey;

            using var client = new HttpClient();
            string response = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);

            var jewelryStore = doc.RootElement
                .GetProperty("shoplifting")
                .GetProperty("jewelry_store");

            return jewelryStore
                .EnumerateArray()
                .All(item => item.GetProperty("disabled").GetBoolean());
        }

        private static void ToastNotification(bool securityDown)
        {
            if (!securityDown)
            {
                new ToastContentBuilder()
                    .AddText("Jewelry store security is down.")
                    .AddText("Cluster ring is available to steal");
                    //.Show();
            }
        }
    }
}