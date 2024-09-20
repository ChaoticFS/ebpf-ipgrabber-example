using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shared
{
    class SeachProxy
    {
        private static readonly HttpClient client = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5262"; // Opdater URL til din API

        static async Task Main(string[] args)
        {
            Console.WriteLine("Search Proxy");
            while (true)
            {
                Console.WriteLine("Enter search terms or 'q' to quit:");
                string input = Console.ReadLine();
                if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) break;

                await SendRequest(input);
            }
        }

        private static async Task SendRequest(string query)
        {
            try
            {
                var response = await client.GetAsync($"{ApiBaseUrl}?query={Uri.EscapeDataString(query)}");

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Response from API:");
                    Console.WriteLine(responseData);
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}