using Finance.Domain.Entities;
namespace Finance.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByNameAsOfDateAsync(string name, DateOnly? asOf = null, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
