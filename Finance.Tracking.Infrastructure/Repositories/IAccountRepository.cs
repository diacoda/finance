using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Finance.Tracking.Infrastructure.Repositories;

public interface IAccountRepository
{
    public Task<int> SaveChangesAsync();
    public Task CreateAccountAsync(Account account);
    public Task<List<Account>> GetAllAccountsWithHoldingsAsync();
    public Task<Account?> GetAccountWithHoldingsAsync(string name);
    public Task UpdateAccount(Account existing, Account updated);
    public Task<IDbContextTransaction> BeginTransactionAsync();
    public Task<List<string>> GetAccountNamesAsync();
}
