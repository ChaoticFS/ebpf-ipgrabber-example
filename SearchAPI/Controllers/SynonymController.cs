using Microsoft.AspNetCore.Mvc;
using Shared.Model;
using SearchAPI.Database;

namespace SearchAPI.Controllers;

[Route("api/")]
[ApiController]
public class SynonymController : ControllerBase
{
    private readonly IDatabase _database;

    public SynonymController(IDatabase database)
    {
        _database = database;
    }

    [HttpGet("synonyms")]
    public IActionResult GetSynonyms(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return BadRequest("Word cannot be empty");
        }

        try
        {
            var synonyms = _database.GetSynonyms(word);
            return Ok(new
            {
                word = word,
                synonyms = synonyms.Select(s => new { id = s.Id, synonym = s.Name })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    
    [HttpGet("synonyms/all")]
    public IActionResult GetAllSynonyms()
    {
        try
        {
            var synonyms = _database.GetAllSynonyms();
            return Ok(synonyms.Select(s => new 
            { 
                id = s.Id, 
                name = s.Name 
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    
    [HttpPost("synonym")]
    public IActionResult PostSynonym(string synonym)
    {
        if (string.IsNullOrWhiteSpace(synonym))
        {
            return BadRequest("Synonym cannot be empty");
        }

        try
        {
            var id = _database.AddSynonym(synonym);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    
    
    

    [HttpPut("synonym")]
    public IActionResult PutSynonym(Synonym synonym)
    {
        if (synonym == null)
        {
            return BadRequest("Synonym cannot be empty");
        }

        try
        {
            _database.UpdateSynonym(synonym);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("synonym")]
    public IActionResult DeleteSynonym(int synonymId)
    {
        try
        {
            _database.DeleteSynonym(synonymId);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("synonym/word")]
    public IActionResult PostSynonymWord([FromQuery] string synonym, string word)
    {
        if (string.IsNullOrEmpty(synonym) || string.IsNullOrEmpty(word))
        {
            return BadRequest("Value cannot be empty");
        }

        try
        {
            _database.AddSynonymWord(synonym, word);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("synonym/word")]
    public IActionResult DeleteSynonymWord([FromQuery] int synonymId, int wordId)
    {
        if (synonymId == 0 || wordId == 0)
        {
            return BadRequest("Id cannot be 0");
        }

        try
        {
            _database.DeleteSynonymWord(synonymId, wordId);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}