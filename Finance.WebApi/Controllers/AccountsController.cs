using Microsoft.AspNetCore.Mvc;
using Finance.Application.Services;
using Finance.Application.DTOs;
namespace Finance.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _svc;
    public AccountsController(IAccountService svc) => _svc = svc;
    [HttpGet("by-name/{name}")]
    public async Task<IActionResult> GetByName(string name)
    {
        var dto = await _svc.GetByNameAsync(name);
        if (dto is null) return NotFound();
        return Ok(dto);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest req)
    {
        var dto = await _svc.CreateAsync(req.Name, req.Owner);
        return CreatedAtAction(nameof(GetByName), new { name = dto.Name }, dto);
    }
    [HttpPost("{id}/holdings")]
    public async Task<IActionResult> AddHolding(System.Guid id, [FromBody] AddHoldingRequest req)
    {
        await _svc.AddHoldingAsync(id, req.Symbol, req.Quantity, req.CostBasis);
        return NoContent();
    }
}
public record CreateAccountRequest(string Name, string Owner);
public record AddHoldingRequest(string Symbol, decimal Quantity, decimal CostBasis);
