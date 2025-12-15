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

            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                _logger.LogInformation("Ctrl+C received. Beginning graceful shutdown...");
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                _logger.LogInformation("JewelryNotice starting up");

                string apiKey = Environment.GetEnvironmentVariable("JewelryNoticeKey");

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("API key not found. Set JewelryNoticeKey environmental variable.");
                    return;
                }

                await Startup(apiKey, cts.Token);

                await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);

                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await PrimaryLoop(apiKey, cts.Token);
                        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
                    }
                    catch (TaskCanceledException) when (cts.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception in primary loop");
                    }
                }
            }
            catch (TaskCanceledException) when (cts.Token.IsCancellationRequested)
            {
                // expected
            }
            finally
            {
                _logger.LogInformation("JewelryNotice shut down cleanly.");
            }
        }

        private static async Task PrimaryLoop(string apiKey, CancellationToken token)
        {
            bool securityOffline = await CheckSecurity(apiKey, token);

            // If store security is offline and it wasn't on the last call, send toast notification
            if (securityOffline && _lastState != securityOffline)
            {
                _logger.LogInformation(
                    "Security state changed: {OldState} > {NewState}",
                    _lastState, securityOffline);
                ToastNotification();
            }
            _lastState = securityOffline;
        }

        private static async Task<bool> CheckSecurity(string apiKey, CancellationToken token)
        {
            try
            {
                _logger.LogDebug("Calling Torn shoplifting API");

                string url = $"https://api.torn.com/torn/?selections=shoplifting&key={apiKey}";

                string response = await _http.GetStringAsync(url, token);

                using var doc = JsonDocument.Parse(response);

                var jewelryStore = doc.RootElement
                    .GetProperty("shoplifting")
                    .GetProperty("jewelry_store");

                bool offline = jewelryStore
                    .EnumerateArray()
                    .All(item => item.GetProperty("disabled").GetBoolean());

                _logger.LogDebug(
                    "Security status checked. Offline = {offline}",
                    offline);

                return offline;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Torn API request timed out.");
                return _lastState ?? false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON returned from Torn API.");
                return _lastState ?? false;
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

        private static async Task Startup(string apiKey, CancellationToken token)
        {
            bool securityOffline = await CheckSecurity(apiKey, token);
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