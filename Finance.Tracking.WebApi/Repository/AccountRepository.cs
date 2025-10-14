using Finance.Tracking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Finance.Tracking.Repository;

public class AccountRepository : IAccountRepository
{
    private readonly FinanceDbContext _dbContext;

    public AccountRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task CreateAccountAsync(Account account)
    {
        var exists = await _dbContext.Accounts.AnyAsync(a => a.Name == account.Name);
        if (exists)
            throw new InvalidOperationException($"Account {account.Name} already exists.");

        await _dbContext.Accounts.AddAsync(account);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<Account>> GetAllAccountsWithHoldingsAsync()
        => await _dbContext.Accounts.Include(a => a.Holdings).ToListAsync();

    public async Task<Account?> GetAccountWithHoldingsAsync(string name)
        => await _dbContext.Accounts.Include(a => a.Holdings)
            .FirstOrDefaultAsync(a => a.Name == name);

    public async Task UpdateAccount(Account existing, Account updated)
    {
        _dbContext.Entry(existing).CurrentValues.SetValues(updated);
        var existingDict = existing.Holdings.ToDictionary(h => h.Symbol);
        var updatedDict = updated.Holdings.ToDictionary(h => h.Symbol);

        foreach (var kvp in updatedDict)
        {
            if (existingDict.TryGetValue(kvp.Key, out var existingHolding))
                existingHolding.Quantity = kvp.Value.Quantity;
            else
            {
                kvp.Value.AccountName = existing.Name;
                existing.Holdings.Add(kvp.Value);
            }
        }

        foreach (var h in existing.Holdings.Where(h => !updatedDict.ContainsKey(h.Symbol)).ToList())
        {
            existing.Holdings.Remove(h);
            _dbContext.Holdings.Remove(h);
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync()
        => await _dbContext.SaveChangesAsync();

    public async Task<IDbContextTransaction> BeginTransactionAsync()
        => await _dbContext.Database.BeginTransactionAsync();

    public async Task<List<string>> GetAccountNamesAsync()
        => await _dbContext.Accounts.Select(a => a.Name).ToListAsync();

}
