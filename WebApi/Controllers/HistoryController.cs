using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Finance.Tracking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService _historyService;

        public HistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        // GET: api/history?days=30
        [HttpGet]
        public async Task<ActionResult> GetHistoricalTotalMarketValue([FromQuery] int? days)
        {
            var history = await _historyService.GetHistoricalTotalMarkeValueAsync(days);
            return Ok(history); // Returns a list of (DateOnly, double) tuples  as JSON array
        }
    }
}