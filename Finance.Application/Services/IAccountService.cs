using Finance.Application.DTOs;
namespace Finance.Application.Services;

public interface IAccountService
{
    Task<AccountDto?> GetByNameAsync(string name, DateOnly? asOf = null, CancellationToken ct = default);
    Task<AccountDto> CreateAsync(string name, string owner, CancellationToken ct = default);
    Task UpdateNameAsync(Guid id, string name, CancellationToken ct = default);
    Task AddHoldingAsync(Guid accountId, string symbol, decimal qty, decimal costBasis, CancellationToken ct = default);
}
