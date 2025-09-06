using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Finance.Tracking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IAccountService _accountService;

        public PortfolioController(IPortfolioService portfolioService, IAccountService accountService)
        {
            _portfolioService = portfolioService;
            _accountService = accountService;
        }

        /// <summary>
        /// GET: api/portfolio/total
        /// </summary>
        /// <param name="asOf"></param>
        /// <returns></returns>
        [HttpGet("total")]
        public async Task<ActionResult<double>> GetTotalMarketValue([FromQuery] DateOnly? asOf = null)
        {
            var total = await _portfolioService.GetTotalMarketValueAsync(asOf);
            return Ok(total);
        }
        /// <summary>
        /// GET: api/portfolio/total/owner/{owner}
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="asOf"></param>
        /// <returns></returns>
        [HttpGet("owner/{owner}")]
        public async Task<ActionResult<double>> GetTotalByOwner(string owner, [FromQuery] DateOnly? asOf = null)
        {
            var total = await _portfolioService.GetTotalMarketValueByOwnerAsync(owner, asOf);
            return Ok(total);
        }

        /// <summary>
        /// GET: api/portfolio/by-owner
        /// </summary>
        /// <param name="asOf"></param>
        /// <returns></returns>
        [HttpGet("by-owner")]
        public async Task<ActionResult<Dictionary<string, double>>> GetTotalGroupedByOwner([FromQuery] DateOnly? asOf = null)
        {
            var totals = await _portfolioService.GetTotalMarketValueGroupedByOwnerAsync(asOf);
            return Ok(totals);
        }

        /// <summary>
        /// GET: api/portfolio/by-owner-type
        /// </summary>
        /// <param name="asOf"></param>
        /// <returns></returns>
        [HttpGet("by-owner-type")]
        public async Task<ActionResult<Dictionary<OwnerTypeKey, double>>> GetTotalByOwnerAndType([FromQuery] DateOnly? asOf = null)
        {
            var totals = await _portfolioService.GetMarketValueByOwnerAndTypeAsync(asOf);
            var result = totals.Select(kvp => new OwnerTypeTotalDto
            {
                Owner = kvp.Key.Owner,
                Type = kvp.Key.Type.ToString(), // enum as string
                Total = kvp.Value
            }).ToList();

            return Ok(result);
        }

        [HttpGet("by-owner-type-accountname")]
        public async Task<ActionResult> GetTotalByOwnerAndTypeWIthAccountNames([FromQuery] DateOnly? asOf = null)
        {
            var totals = await _accountService.GetTotalMarketValueGroupedByWithNamesAsync(
                a => new OwnerTypeKey(a.Owner, a.Type),
                asOf
            );

            var result = totals.Select(kvp => new OwnerTypeAccountNameDTO
            {
                Owner = kvp.Key.Owner,
                Type = kvp.Key.Type.ToString(),
                Total = kvp.Value.Total,
                AccountNames = kvp.Value.AccountNames
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// GET: api/portfolio/mv/byowner-filter
        /// </summary>
        /// <param name="asOf"></param>
        /// <returns></returns>
        [HttpGet("by-owner-filter")]
        public async Task<ActionResult<List<OwnerFilterTotalDto>>> GetTotalByOwnerAndFilter([FromQuery] DateOnly? asOf = null)
        {
            var totals = await _portfolioService.GetMarketValueByOwnerAndAccountFilterAsync(asOf);

            var result = totals.Select(kvp => new OwnerFilterTotalDto
            {
                Owner = kvp.Key.Owner,
                AccountFilter = kvp.Key.AccountFilter.ToString(), // enum as string
                Total = kvp.Value
            }).ToList();

            return Ok(result);
        }
        /// <summary>
        /// POST: api/portfolio/query
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost("query")]
        public async Task<ActionResult<double>> GetTotalByQuery([FromBody] AccountSummaryQuery query)
        {
            // Build expression dynamically
            Expression<Func<AccountSummary, bool>> expr = a => true;

            if (!string.IsNullOrEmpty(query.Owner))
                expr = expr.AndAlso(a => a.Owner == query.Owner);

            if (query.AccountFilter.HasValue)
                expr = expr.AndAlso(a => a.AccountFilter == query.AccountFilter.Value);

            var total = await _portfolioService.GetTotalMarketValueWhereExpressionAsync(expr, query.AsOf);
            return Ok(total);
        }
    }








}
