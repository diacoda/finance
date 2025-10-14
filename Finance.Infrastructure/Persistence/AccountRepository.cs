using Microsoft.EntityFrameworkCore;
using Finance.Domain.Repositories;
using Finance.Domain.Entities;
using Finance.Infrastructure.Persistence;
namespace Finance.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;
    public AccountRepository(AppDbContext db) => _db = db;
    public async Task AddAsync(Account account, CancellationToken ct = default)
    {
        _db.Add(account);
        await _db.SaveChangesAsync(ct);
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var acc = await _db.Accounts.FindAsync(new object[] { id }, ct);
        if (acc is null) return;
        _db.Accounts.Remove(acc);
        await _db.SaveChangesAsync(ct);
    }
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => EF.Property<Guid>(a, "Id") == id, ct);
    }
    public async Task<Account?> GetByNameAsOfDateAsync(string name, DateOnly? asOf = null, CancellationToken ct = default)
    {
        return await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Name == name, ct);
    }
    public async Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        _db.Update(account);
        await _db.SaveChangesAsync(ct);
    }
}
