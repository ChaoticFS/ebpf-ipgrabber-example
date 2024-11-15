using System;
using System.Collections.Generic;

namespace Shared.Model;
public class SearchResponse
{
    public List<SearchResult> Results { get; set; }
    public int Count { get; set; }
    public List<string> IgnoredWords { get; set; }
}

public class SearchResult
{
    public int MId { get; set; }
    public string MUrl { get; set; }
    public string MIdxTime { get; set; }
    public string MCreationTime { get; set; }
    
    public string GetShortUrl()
    {
        var parts = MUrl.Split("medium", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? "medium" + parts[1] : MUrl; // Hvis "medium" findes, returner den del, der følger
    }
}