using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Finance.Tracking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PricesController : ControllerBase
    {
        private readonly IPricingService _pricingService;

        public PricesController(IPricingService pricingService)
        {
            _pricingService = pricingService;
        }

        /// <summary>
        /// Get all prices for a specific date.
        /// If no date is provided, defaults to today.
        /// </summary>
        /// <param name="date">Optional date in yyyy-MM-dd format</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PriceDTO>>> GetPricesByDate([FromQuery] DateOnly? asOf = null)
        {
            var prices = await _pricingService.GetPricesByDateAsync(asOf);
            var dtos = prices.Select(p => new PriceDTO
            {
                Symbol = p.Symbol,
                Date = p.Date,
                Value = p.Value
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Saves or updates a single price entry.
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PriceDTO price)
        {
            if (price == null)
                return BadRequest("Price data is required.");

            var result = await _pricingService.SavePriceAsync(
                price.Symbol,
                price.Value,
                price.Date
            );

            if (result > 0)
                return Ok(new { message = "Price saved successfully." });

            return StatusCode(500, "Failed to save price.");
        }
    }
}
