using Microsoft.AspNetCore.Mvc;
using Finance.Tracking.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Finance.Tracking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet("names")]
    public List<string> GetAccountNames()
    {
        return _accountService.GetAccountNames();
    }

    [HttpGet("names/{accountName}")]
    public async Task<ActionResult<AccountDTO>> GetAccountDetails(string accountName)
    {
        Account? account = await _accountService.GetAccountAsync(accountName);
        if (account is null)
            return NotFound($"Account '{accountName}' not found.");

        var dto = new AccountDTO
        {
            Name = account.Name,
            Cash = account.Cash,
            Holdings = account.Holdings
                .Select(h => new HoldingDTO
                {
                    Symbol = h.Symbol,
                    Quantity = h.Quantity
                })
                .ToList(),
            MarketValue = account.MarketValue
        };

        return Ok(dto);
    }

    [HttpPost("summaries")]
    public async Task<IActionResult> BuildSummaries([FromQuery] DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        await _accountService.BuildSummariesByDateAsync(date);
        return Ok(new { Message = $"Account summaries built for {date:yyyy-MM-dd}" });
    }

    [HttpGet("latest-dates")]
    public async Task<ActionResult<List<string>>> GetLatestDates()
    {
        var dates = await _accountService.GetLast30AvailableDatesAsync();
        return Ok(dates.Select(d => d.ToString("yyyy-MM-dd")).ToList());
    }

    [HttpDelete("summaries")]
    public async Task<IActionResult> DeleteSummaries([FromQuery] DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var deletedCount = await _accountService.DeleteSummariesByDateAsync(date);
        return Ok(new { deleted = deletedCount, date = date.ToString("yyyy-MM-dd") });
    }
}
