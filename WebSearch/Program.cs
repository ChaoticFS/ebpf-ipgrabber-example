using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebSearch;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Scrapping this for now, routing via proxy is bugging out in a way i dont know how to fix
// if (builder.HostEnvironment.Environment == "Production")
// { // This lets nginx handle requests when deployed to kubernetes
//     builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:80") });
// }
// else
// {
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5262") });
// }

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();