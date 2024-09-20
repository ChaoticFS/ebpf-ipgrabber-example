using System;
using Shared;
using Shared.Model;



namespace SearchAPI;

public interface ISearchLogic
{
    bool IsCaseSensitive { get; set; }
    SearchResult Search(String[] query, int maxAmount);
}

