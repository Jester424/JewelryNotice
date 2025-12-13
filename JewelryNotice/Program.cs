using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using Newtonsoft.Json;

namespace JewelryNotice
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, world!");
            bool securityOffline = await CheckSecurity();

            if (!securityOffline)
            {
                Console.WriteLine("Security is up");
            }
        }

        private static async Task<bool> CheckSecurity()
        {
            string url = "https://api.torn.com/torn/plR8x8N3ls16LX9k?selections=shoplifting&key=Dw88mnzdwKgRgHk8";

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
    }
}