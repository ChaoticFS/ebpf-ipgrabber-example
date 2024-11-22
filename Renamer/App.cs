using Microsoft.Extensions.Configuration;

namespace Renamer;
public class App
	{
    private IConfiguration _configuration;
    private Crawler _crawler;
    public int CountFiles { get; private set; }

    public App(IConfiguration configuration, Crawler crawler)
    {
        _configuration = configuration;
        _crawler = crawler;
    }

    public void Run()
    {
        if (_configuration["SKIP_PROCESSING"] == "true")
        {
            Console.WriteLine("Database configuration already done, skipping as instructed");
            return;
        }

        _crawler.Crawl(new DirectoryInfo(_configuration["Database:Folder"]), RenameFile);
        Console.WriteLine("Done with");
        Console.WriteLine("Folders: " + _crawler.CountFolders);
        Console.WriteLine("Files:   " + CountFiles);
    }

    void RenameFile(FileInfo f)
    {
        Console.WriteLine($"Behandler {f.FullName}");

        if (f.FullName.EndsWith(".txt")) return;

        if (f.Name.StartsWith('.')) return;


        var ending = f.FullName.EndsWith(".") ? "txt" : ".txt";

        File.Move(f.FullName, f.FullName + ending, true);

        CountFiles++;
    }
}