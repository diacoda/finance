using Microsoft.AspNetCore.Mvc;
using Finance.Tracking.Services;
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
