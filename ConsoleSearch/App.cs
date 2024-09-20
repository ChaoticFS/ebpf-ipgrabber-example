using System;
using System.Net.Http;
using System.Threading.Tasks;
using Shared;

namespace ConsoleSearch
{
    public class App
    {
        private static readonly HttpClient client = new HttpClient();

        private Config _config = new Config();

        public App()
        {
            // Set the base address to the API's base URL
            client.BaseAddress = new Uri("http://localhost:5262/");
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Console Search");

            while (true)
            {
                Console.WriteLine("Enter search terms - 'q' to quit:");
                string input = Console.ReadLine();
                if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) break;

                var query = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var queryString = string.Join(",", query);

                try
                {
                    // Make the HTTP GET request to the SearchAPI
                    var s = $"api/search?query={Uri.EscapeDataString(queryString)}";
                    var response = await client.GetAsync(s);
                    response.EnsureSuccessStatusCode();
                    var responseData = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Search results:");
                    Console.WriteLine(responseData);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Request error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}