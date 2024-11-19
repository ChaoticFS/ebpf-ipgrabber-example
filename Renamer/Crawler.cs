namespace Renamer;
public class Crawler
{
    private int countFolders;
    public delegate void FileHandler(FileInfo f);

    public void Crawl(DirectoryInfo dir, FileHandler aFileHandler)
    {
        Console.WriteLine("Crawling " + dir.FullName);

        foreach (var file in dir.EnumerateFiles())
        {
            aFileHandler(file);
        }

        foreach (var d in dir.EnumerateDirectories())
        {
            Crawl(d, aFileHandler);
        }

        countFolders++;
    }

    public int CountFolders => countFolders;
}

