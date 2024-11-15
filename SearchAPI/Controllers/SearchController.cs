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
    public IActionResult SearchDocuments([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty");

        // Split the query string into individual search terms
        var searchTerms = query.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Get word IDs from search terms
        var wordIds = _database.GetWordIds(searchTerms, out var ignoredWords);

        // Fetch documents containing the search terms
        var docIds = _database.GetDocuments(wordIds)
            .Select(kvp => kvp.Key)
            .ToList();

        var documents = _database.GetDocDetails(docIds);

        var result = documents.Select(document => new BEDocument
        {
            mUrl = document.mUrl,
            mIdxTime = document.mIdxTime,
            mId = document.mId,
            mCreationTime = document.mCreationTime
        }).ToList();

        return Ok(new
        {
            Results = result,
            Count = result.Count,
            IgnoredWords = ignoredWords
        });
    }
}