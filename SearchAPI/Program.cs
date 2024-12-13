using Shared;
using SearchAPI.Database;
using SearchAPI.Services;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();

        builder.AddMeter("Microsoft.AspNetCore.Hosting",
                         "Microsoft.AspNetCore.Server.Kestrel");
        builder.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                       0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
    });

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

if (!string.IsNullOrEmpty(redisConnectionString))
{
    Console.Write("Initializing Redis Cache");
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else
{
    Console.WriteLine("Initializing In Memory Cache");
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
}

var rqliteConnectionString = builder.Configuration["Database:ConnectionString"];

if (!string.IsNullOrEmpty(rqliteConnectionString))
{
    Console.Write("Initializing Rqlite Database");
    builder.Services.AddSingleton<IDatabase, RqliteDatabase>();
}
else
{
    Console.WriteLine("Initializing Local Database");
    builder.Services.AddSingleton<IDatabase, LocalDatabase>();
}

builder.Services.AddSingleton<ConfigModel>();

//  CORS-politik for at tillade anmodninger fra Blazor
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() // This should be changed to a single port once we get k8s up and running
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPrometheusScrapingEndpoint();

app.MapGet("/telemetry", () => "OpenTelemetry! ticks:"
                     + DateTime.Now.Ticks.ToString()[^5..]);

app.UseCors(); //Activating Cors

app.UseAuthorization();

app.MapControllers();

app.Run();