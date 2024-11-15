using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shared;
using Shared.Model;

namespace ConsoleSearch
{
    public class App
    {
        private static readonly HttpClient client = new HttpClient();
        private ConfigModel _config = new ConfigModel();

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
                Console.WriteLine("Enter search terms - 'q' to quit: - 'cs' to toggle case sensitivity");
                string input = Console.ReadLine();

                if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) 
                {
                    break;
                }

                if (input.Equals("cs"))
                {
                    _config.CaseSensitive = !_config.CaseSensitive;  
                    Console.WriteLine("Case sensitivity is now " + (_config.CaseSensitive ? "ON" : "OFF"));
                    continue;
                }

                var query = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var queryString = string.Join(",", query);

                try
                {
                    // Make the HTTP GET request to the SearchAPI
                    var s = $"api/search?query={Uri.EscapeDataString(queryString)}";
                    var response = await client.GetAsync(s);
                    response.EnsureSuccessStatusCode();
                    var responseData = await response.Content.ReadAsStringAsync();

                    // Deserialize the JSON response
                    var searchResponse = JsonConvert.DeserializeObject<SearchResponse>(responseData);

                    // Display the search results in a formatted way
                    Console.WriteLine($"Found {searchResponse.Count} result(s):");

                    // Print table header
                    Console.WriteLine($"{"ID",-10} {"URL",-60} {"Index Time",-20} {"Creation Time",-20}");

                    // Print each result
                    foreach (var result in searchResponse.Results)
                    {
                        Console.WriteLine($"{result.MId,-10} {result.GetShortUrl(),-60} {result.MIdxTime,-20} {result.MCreationTime,-20}");
                    }

                    if (searchResponse.IgnoredWords.Count > 0)
                    {
                        Console.WriteLine("\nIgnored words:");
                        Console.WriteLine(string.Join(", ", searchResponse.IgnoredWords));
                    }
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
