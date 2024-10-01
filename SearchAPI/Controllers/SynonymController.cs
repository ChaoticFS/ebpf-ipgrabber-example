using Microsoft.AspNetCore.Mvc;
using Shared.Model;
using SearchAPI.Database;
using System.Collections.Generic;
using System.Linq;

namespace SearchAPI.Controllers
{
    [Route("api/")]
    [ApiController]
    public class SynonymController : ControllerBase
    {
        private readonly IDatabase _database;

        public SynonymController(IDatabase database)
        {
            _database = database;
        }
        
        // [HttpGet("synonyms")]
        // public IActionResult GetSynonyms([FromQuery] string word)
        // {
        //     if (string.IsNullOrWhiteSpace(word))
        //         return BadRequest("Word cannot be empty");
        //
        //     var synonyms = _database.GetSynonyms(word);
        //
        //     var result = new
        //     {
        //         word = word,
        //         synonyms = synonyms
        //     };
        //
        //     return Ok(result);
        // }
        
        [HttpGet("synonyms")]
        public async Task<IActionResult> GetSynonyms([FromQuery] string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return BadRequest("Word cannot be empty");

            try
            {
                var synonyms = await _database.GetSynonymsFromApi(word);
                return Ok(new
                {
                    word = word,
                    synonyms = synonyms.Select(s => new { synonym = s.Synonym, weight = s.Weight })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }




        

    }
    
    
    
    
    
    
}