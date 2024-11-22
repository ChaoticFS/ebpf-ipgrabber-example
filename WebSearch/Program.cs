using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebSearch;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

if (builder.HostEnvironment.Environment == "Production")
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://searchapi-service:5262") });
}
else
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5262") });
}

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


await builder.Build().RunAsync();