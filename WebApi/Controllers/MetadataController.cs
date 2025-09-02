using Microsoft.AspNetCore.Mvc;
using Finance.Tracking.Models;
using Microsoft.AspNetCore.Authorization;

namespace Finance.Tracking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class MetadataController : ControllerBase
{
    // GET: api/metadata/enums
    [HttpGet("enums")]
    public ActionResult GetEnums()
    {
        var accountTypes = Enum.GetValues(typeof(AccountType))
            .Cast<AccountType>()
            .Select(e => new EnumValueDto { Name = e.ToString(), Value = (int)e })
            .ToList();

        var accountFilters = Enum.GetValues(typeof(AccountFilter))
            .Cast<AccountFilter>()
            .Select(e => new EnumValueDto { Name = e.ToString(), Value = (int)e })
            .ToList();

        return Ok(new
        {
            AccountTypes = accountTypes,
            AccountFilters = accountFilters
        });
    }

    // GET: api/metadata/symbols
    [HttpGet("symbols")]
    public ActionResult GetSymbols()
    {
        var symbols = Enum.GetNames(typeof(Symbol));
        return Ok(symbols);
    }
}
