namespace Finance.Application.Services;

using Finance.Domain.Repositories;
using Finance.Domain.Entities;
using Finance.Domain.ValueObjects;
using Finance.Application.DTOs;
public class AccountService : IAccountService
{
    private readonly IAccountRepository _repo;
    public AccountService(IAccountRepository repo) => _repo = repo;
    public async Task<AccountDto> CreateAsync(string name, string owner, CancellationToken ct = default)
    {
        var acc = new Account(name, owner);
        await _repo.AddAsync(acc, ct);
        return Map(acc);
    }
    public async Task<AccountDto?> GetByNameAsync(string name, DateOnly? asOf = null, CancellationToken ct = default)
    {
        var acc = await _repo.GetByNameAsOfDateAsync(name, asOf, ct);
        return acc == null ? null : Map(acc);
    }
    public async Task UpdateNameAsync(Guid id, string name, CancellationToken ct = default)
    {
        var acc = await _repo.GetByIdAsync(id, ct);
        if (acc is null) throw new KeyNotFoundException("account not found");
        acc.UpdateName(name);
        await _repo.UpdateAsync(acc, ct);
    }
    public async Task AddHoldingAsync(Guid accountId, string symbol, decimal qty, decimal costBasis, CancellationToken ct = default)
    {
        var acc = await _repo.GetByIdAsync(accountId, ct);
        if (acc is null) throw new KeyNotFoundException("account not found");
        var holding = new Holding(new Symbol(symbol), qty, new Money(costBasis));
        acc.AddHolding(holding);
        await _repo.UpdateAsync(acc, ct);
    }
    private static AccountDto Map(Account a)
    {
        var holdings = a.Holdings.Select(h => new HoldingDto(h.Symbol.ToString(), h.Quantity, h.CostBasis.Amount));
        return new AccountDto(a.Id, a.Name, a.Owner, holdings);
    }
}
