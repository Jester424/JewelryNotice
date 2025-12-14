using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;


namespace JewelryNotice
{
    class Program
    {
        private static ILogger<Program> _logger;

        private static readonly HttpClient _http = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static bool? _lastState = null;

        static async Task Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)    
                    .AddConsole();
            });

            _logger = loggerFactory.CreateLogger<Program>();

            _logger.LogInformation("JewelryNotice starting up");

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
                _logger.LogInformation($"Security state changed: {_lastState} > {securityOffline}");
                ToastNotification();
            }
            _lastState = securityOffline;
        }

        private static async Task<bool> CheckSecurity(string apiKey)
        {
            try
            {
                _logger.LogDebug("Calling Torn shoplifting API");

                string url = $"https://api.torn.com/torn/?selections=shoplifting&key={apiKey}";

                string response = await _http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);

                var jewelryStore = doc.RootElement
                    .GetProperty("shoplifting")
                    .GetProperty("jewelry_store");

                bool offline = jewelryStore
                    .EnumerateArray()
                    .All(item => item.GetProperty("disabled").GetBoolean());

                _logger.LogInformation($"Security status checked. Offline = {offline}");

                return offline;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected failure while calling Torn API.");
                return _lastState ?? false;
            }
        }

        private static void ToastNotification()
        {
            new ToastContentBuilder()
                .AddText("Jewelry store security is down.")
                .AddText("Cluster ring is available to steal.")
                .Show();
        }

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