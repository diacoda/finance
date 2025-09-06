using System.Linq.Expressions;

namespace Finance.Tracking.Services;

public interface IAccountService
{
    public Task InitializeAsync();
    public List<string> GetAccountNames();
    public Task<Account?> GetAccountAsync(string accountName);
    public Task UpdateAccountAsync(Account account);
    Task BuildSummariesByDateAsync(DateOnly? asOf = null);
    Task<List<DateOnly>> GetLast30AvailableDatesAsync();
    Task<List<AccountSummary>> GetAccountSummariesAsync(DateOnly asOf);
    Task<int> DeleteSummariesByDateAsync(DateOnly? asOf = null);
    Task<double> GetTotalMarketValueAsync(DateOnly asOf);
    Task<double> GetTotalMarketValueWherePredicateAsync(Expression<Func<AccountSummary, bool>> predicate, DateOnly asOf);
    Task<Dictionary<TKey, double>> GetTotalMarketValueGroupedByAsync<TKey>(Expression<Func<AccountSummary, TKey>> keySelector, DateOnly? asOf = null) where TKey : notnull;
}