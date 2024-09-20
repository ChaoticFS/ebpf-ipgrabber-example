using System;
using Shared;
using Shared.Model;



namespace Shared;

public interface ISearchLogic
{
    bool IsCaseSensitive { get; set; }
    SearchResult Search(String[] query, int maxAmount);
}

