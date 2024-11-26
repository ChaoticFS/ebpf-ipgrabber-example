using System;

namespace Shared.Model;
public class ResultDocument
{
    public int Id { get; set; }

    public String Url { get; set; }

    public String IdxTime { get; set; }

    public String CreationTime { get; set; }

    public int Count { get; set; }
    
    public string GetShortUrl()
    {
        var parts = Url.Split("medium", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? "medium" + parts[1] : Url; // Hvis "medium" findes, returner den del, der følger
    }
}