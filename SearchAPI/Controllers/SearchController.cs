using Microsoft.AspNetCore.Mvc;
using Shared.Model;
using SearchAPI.Database;

namespace SearchAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly IDatabase _database;

    public SearchController(IDatabase database)
    {
        _database = database;
    }

    [HttpGet]
    public IActionResult SearchDocuments(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty");

        // Split the query string into individual search terms
        var searchTerms = query.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Get word IDs from search terms
        var wordIds = _database.GetWordIds(searchTerms, out var ignoredWords);

        // Fetch documents containing the search terms
        var docIdOcc = _database.GetDocuments(wordIds);

        var result = _database.GetDocDetails(docIdOcc);

        return Ok(new
        {
            Results = result,
            Count = result.Count,
            IgnoredWords = ignoredWords
        });
    }

    [HttpGet("id")]
    public IActionResult GetIdFromWords(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty");

        // Split the query string into individual search terms
        var searchTerms = query.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Get word IDs from search terms
        var result = _database.GetWordIds(searchTerms, out var ignoredWords);


        //Synonym integration 
        //Fetch synonyms for each search term
        var synonymIds = new List<int>();
        foreach (var term in searchTerms)
        {
            var synonyms = _database.GetSynonyms(term);
            synonymIds.AddRange(synonyms.Select(s => s.Id));
        }
        
        //Combine word IDs and synonym IDs, ensuring no duplicates
        var allIds = result.Concat(synonymIds).Distinct().ToList();
        
        
        return Ok(new
        {
            Results = allIds,
            Count = result.Count,
            IgnoredWords = ignoredWords
        });
        
    }
}