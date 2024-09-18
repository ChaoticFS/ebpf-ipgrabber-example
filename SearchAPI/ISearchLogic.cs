using System;
using Shared.Model;


namespace ConsoleSearch;

public interface ISearchLogic
{
    bool IsCaseSensitive { get; set; }
    SearchResult Search(String[] query, int maxAmount);
}

