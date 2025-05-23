﻿using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indexer;
class Program
{
    static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration["Database:ConnectionString"];

                if (!string.IsNullOrEmpty(connectionString))
                {
                    Console.Write("Initializing Rqlite Database");
                    services.AddSingleton<IDatabase, RqliteDatabase>();
                }
                else
                {
                    Console.WriteLine("Initializing Local Database");
                    services.AddSingleton<IDatabase, LocalDatabase>();
                }

                services.AddSingleton<IDatabase, LocalDatabase>();
                services.AddScoped<Crawler>();
                services.AddScoped<App>();
            })
            .Build();

        var app = host.Services.GetRequiredService<App>();
        app.Run();
    }
}