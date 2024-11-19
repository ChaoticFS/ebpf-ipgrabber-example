using Microsoft.AspNetCore.Mvc;

namespace LoadBalancerAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoadBalancerController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    // List of Search API instances
    private readonly string[] _searchApiInstances = new[]
    {
        "http://localhost:5262/api/search",
        "http://localhost:5263/api/search",
        "http://localhost:5264/api/search"
    };

    public LoadBalancerController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> LoadBalancedSearch([FromQuery] string query)
    {
        // Simple round-robin load balancing
        var instance = _searchApiInstances[GetNextInstanceIndex()];

        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync($"{instance}?query={query}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
    
            // Create a response object to include both the content and the instance
            var responseObject = new
            {
                Content = content,
                InstanceUsed = instance // Include the instance URL in the response
            };

            return Ok(responseObject);
        }

        return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }


    private int GetNextInstanceIndex()
    {
        // Logic for round-robin (store the current index in memory or in a shared location)
        // This is a simple example. For production, consider more robust methods
        return (int)(DateTime.UtcNow.Ticks % _searchApiInstances.Length);
    }
}