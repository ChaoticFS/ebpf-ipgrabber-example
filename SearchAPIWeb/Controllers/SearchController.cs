using Microsoft.AspNetCore.Mvc;

namespace SearchAPI.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchLogic _searchLogic;

        public SearchController(ISearchLogic searchLogic)
        {
            _searchLogic = searchLogic;
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty");

            var searchTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var result = _searchLogic.Search(searchTerms, 10); // Eks. med maks 10 resultater

            return Ok(result);
        }
    }
}