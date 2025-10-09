using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
    public async Task<ActionResult<AccountDTO>> GetAccountDetails(string accountName, DateOnly? date)
    {
        Account? account = await _accountService.GetAccountByDateAsync(accountName, date);
        if (account is null)
            return NotFound($"Account '{accountName}' not found.");

        // Compute cash from holdings
        double cash = account.Holdings
            .Where(h => h.Symbol == Symbol.CASH)
            .Sum(h => h.Quantity);

        var dto = new AccountDTO
        {
            Name = account.Name,
            Cash = cash, //account.Cash,
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

    [HttpPut("names/{accountName}")]
    public async Task<IActionResult> UpdateAccountDetails(string accountName, [FromBody] AccountDTO accountDTO)
    {
        if (accountDTO == null)
            return BadRequest("Account data is required.");

        if (!string.Equals(accountName, accountDTO.Name, StringComparison.OrdinalIgnoreCase))
        {
            // optional: prevent mismatch between URL and payload
            accountDTO.Name = accountName;
        }

        try
        {
            var account = MappingExtensions.MapDtoToAccount(accountDTO);
            account = await _accountService.UpdateAccountAsync(account);
            if (account == null)
                return StatusCode(500, $"Internal Server Error: account null after update.");

            var dto = MappingExtensions.MapAccountToDto(account);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating account {accountName}: {ex}");
            return StatusCode(500, $"Internal Server Error: {ex.Message}");
        }
    }

    [HttpPost("summaries")]
    public async Task<IActionResult> BuildSummaries([FromQuery] DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        await _accountService.BuildSummariesByDateAsync(date);
        return Ok(new { Message = $"Account summaries built for {date:yyyy-MM-dd}" });
    }

    [HttpGet("latest-dates/{days}")]
    public async Task<ActionResult<List<string>>> GetLatestDates(int days = 30)
    {
        if (days <= 0)
            return BadRequest("Days must be a positive integer.");

        var dates = await _accountService.GetLastAvailableDatesAsync(days);
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
