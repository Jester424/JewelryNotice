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

            while (1 == 1)
            {
                MainLoop(apiKey);
                Thread.Sleep(10000);
            }
        }

        private static async Task MainLoop(string apiKey)
        {
            bool securityOffline = await CheckSecurity(apiKey);
            ToastNotification(securityOffline);
        }

        private static async Task<bool> CheckSecurity(string apiKey)
        {
            try
            {
                string url = $"https://api.torn.com/torn/?selections=shoplifting&key={apiKey}";

                string response = await _http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);

                var jewelryStore = doc.RootElement
                    .GetProperty("shoplifting")
                    .GetProperty("jewelry_store");

                return jewelryStore
                    .EnumerateArray()
                    .All(item => item.GetProperty("disabled").GetBoolean());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CheckSecurity failed: {ex.Message}");
                throw;
            }
        }

        private static void ToastNotification(bool securityDown)
        {
            if (!securityDown)
            {
                new ToastContentBuilder()
                    .AddText("Jewelry store security is down.")
                    .AddText("Cluster ring is available to steal.")
                    .Show();
            }
        }

        private static readonly HttpClient _http = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }
}