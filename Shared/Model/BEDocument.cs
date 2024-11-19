using System;

namespace Shared.Model;
public class BEDocument
{
    public int mId { get; set; }

    public String mUrl { get; set; }

    public String mIdxTime { get; set; }

    public String mCreationTime { get; set; }
    
    public string GetShortUrl()
    {
        var parts = mUrl.Split("medium", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? "medium" + parts[1] : mUrl; // Hvis "medium" findes, returner den del, der følger
    }
}