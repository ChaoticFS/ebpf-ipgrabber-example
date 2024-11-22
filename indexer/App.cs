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
            if(_configuration["SKIP_PROCESSING"] == "true")
            {
                Console.WriteLine("Database configuration already done, skipping as instructed");
                return;
            }

            var root = new DirectoryInfo(_configuration["Database:Folder"]);

            DateTime start = DateTime.Now;

            _crawler.IndexFilesIn(root, new List<string> { ".txt" });

            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);

            var all = _db.GetAllWordCounts();

            Console.WriteLine($"Indexed {_db.GetDocumentCounts()} documents");
            Console.WriteLine($"Number of different words: {all.Count}");
        }
    }
}
