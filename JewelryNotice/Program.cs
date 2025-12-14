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

            await Startup(apiKey);

            await Task.Delay(TimeSpan.FromSeconds(10));

            while (true)
            {
                try
                {
                    await MainLoop(apiKey);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Loop error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private static async Task MainLoop(string apiKey)
        {
            bool securityOffline = await CheckSecurity(apiKey);

            // If store security is offline and it wasn't on the last call, send toast notification
            if (securityOffline && _lastState != securityOffline)
            {
                ToastNotification();
            }
            _lastState = securityOffline;
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

        private static void ToastNotification()
        {
            new ToastContentBuilder()
                .AddText("Jewelry store security is down.")
                .AddText("Cluster ring is available to steal.")
                .Show();
        }

        private static readonly HttpClient _http = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static bool? _lastState = null;

        private static async Task Startup(string apiKey)
        {
            bool securityOffline = await CheckSecurity(apiKey);
            _lastState = securityOffline;
            if (securityOffline)
            {
                ToastNotification();
            }
            else
            {
                new ToastContentBuilder()
                    .AddText("API successfully called.")
                    .AddText("Everything appears to be working correctly.")
                    .Show();
            }
        }
    }
}