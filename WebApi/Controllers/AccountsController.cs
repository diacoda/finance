using Microsoft.AspNetCore.Mvc;
using Finance.Tracking.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Finance.Tracking.Models;

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

    private Account MapDtoToAccount(AccountDTO dto)
    {
        var parts = dto.Name.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            throw new ArgumentException($"Invalid account key: {dto.Name}");

        var parsedType = Enum.Parse<AccountType>(parts[2], true);

        var account = new Account
        {
            Name = dto.Name,
            Owner = parts[0],
            Bank = Enum.Parse<Bank>(parts[1], true),
            Type = parsedType,
            AccountFilter = parsedType.ToAccountFilter(),
            MarketValue = 0,
            Currency = Enum.Parse<Currency>(parts[3], true),
            Cash = dto.Cash,
            Holdings = dto.Holdings.Select(h => new Holding
            {
                Symbol = h.Symbol,           // no parsing needed
                Quantity = h.Quantity,
                AccountName = dto.Name
            }).ToList()
        };
        return account;
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
            var account = MapDtoToAccount(accountDTO);
            await _accountService.UpdateAccountAsync(account);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while updating the account.");
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
