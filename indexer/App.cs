using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Shared;

namespace Indexer
{
    public class App
    {
        private IConfiguration _configuration;
        private IDatabase _db;
        private Crawler _crawler;

        public App(IConfiguration configuration, IDatabase db, Crawler crawler)
        {
            _configuration = configuration;
            _db = db;
            _crawler = crawler;
        }

        public void Run()
        {
            var root = new DirectoryInfo(_configuration["Database:Folder"]);

            DateTime start = DateTime.Now;

            _crawler.IndexFilesIn(root, new List<string> { ".txt" });

            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);

            var all = _db.GetAllWordCounts();

            Console.WriteLine($"Indexed {_db.GetDocumentCounts()} documents");
            Console.WriteLine($"Number of different words: {all.Count}");

            // Ask user for many results they would like to see.
            int resultCount = GetResultCount(all.Count);

            // Check if the user wants to include timestamps - on or off
            bool includeTimestamp = GetUserPreference();

            foreach (var p in all.Take(resultCount))
            {
                string output = $"<{p.Id},{p.Value}> forekommer {p.Count} gange";
                if (includeTimestamp)
                {
                    var doc = _db.GetDocumentById(p.Id);
                    output += $" (indekseret kl. {doc.mIdxTime})";
                }

                Console.WriteLine(output);
            }
        }

        private bool GetUserPreference()
        {
            // Simulate user input or get it from actual input
            Console.WriteLine("Ønsker du tidsstempel? Skriv /timestamp=on for at inkludere, /timestamp=off for at udelade:");
            string userInput = Console.ReadLine();

            if (userInput == "/timestamp=on")
            {
                return true;
            }
            else if (userInput == "/timestamp=off")
            {
                return false;
            }
            else
            {
                Console.WriteLine("Ugyldigt input. Brug /timestamp=on eller /timestamp=off.");
                return GetUserPreference();
            }
        }
        
        private int GetResultCount(int totalCount)
        {
            Console.WriteLine("Angiv antal resultater: fx /results=15 for at få de 15 bedste, eller /results=all for at få vist alle:");
            string userInput = Console.ReadLine();

            if (userInput.StartsWith("/results="))
            {
                string resultInput = userInput.Replace("/results=", "");

                if (resultInput == "all")
                {
                    return totalCount; // Return all results
                }
                else if (int.TryParse(resultInput, out int resultCount))
                {
                    return Math.Min(resultCount, totalCount); // Return the user-defined number of results
                }
                else
                {
                    Console.WriteLine("Ugyldigt input. Forsøger igen.");
                    return GetResultCount(totalCount);
                }
            }
            else
            {
                Console.WriteLine("Ugyldigt input. Brug kommandoen /results=.");
                return GetResultCount(totalCount);
            }
        }
        
        
    }
}
