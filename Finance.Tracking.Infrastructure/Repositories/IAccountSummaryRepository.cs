namespace Finance.Tracking.Infrastructure.Repositories;

using System.Linq.Expressions;
public interface IAccountSummaryRepository
{
    public Task<List<DateOnly>> GetLast30AvailableDatesAsync();
    public Task<Dictionary<(string Name, DateOnly Date, AssetClass AssetClass), AccountSummary>> GetSummariesByDateAsync(DateOnly date);
    public Task<List<AccountSummary>> GetSummariesForAccountAsync(string accountName, DateOnly date);
    public Task<List<AccountSummary>> GetSummariesByDateRawAsync(DateOnly date);
    public Task AddSummaryAsync(AccountSummary summary);
    public Task<DateOnly?> GetLatestDateAsync();
    public Task<int> DeleteSummariesByDateAsync(DateOnly date);
    public Task<double> GetTotalMarketValueAsync(DateOnly asOf);
    public Task<double> GetTotalMarketValueWherePredicateAsync(Expression<Func<AccountSummary, bool>> predicate, DateOnly asOf);
    public Task<Dictionary<AssetClass, double>> GetTotalMarketValueByAssetClassAsync(DateOnly asOf);
    public Task<Dictionary<GroupKey<string, AssetClass>, double>> GetTotalMarketValueByOwnerAndAssetClassAsync(DateOnly asOf);
    public Task<Dictionary<TKey, MarketValueGroup>> GetTotalMarketValueGroupedByWithNamesAsync<TKey>(
        Expression<Func<AccountSummary, TKey>> keySelector,
        DateOnly asOf)
        where TKey : notnull;
    public Task<Dictionary<TKey, double>> GetTotalMarketValueGroupedByAsync<TKey>(
        Expression<Func<AccountSummary, TKey>> keySelector,
        DateOnly asOf) where TKey : notnull;
    public Task<Dictionary<GroupKey<T1, T2>, double>> GetTotalMarketValueGroupedBy2Async<T1, T2>(
        Expression<Func<AccountSummary, T1>> key1,
        Expression<Func<AccountSummary, T2>> key2,
        DateOnly asOf)
        where T1 : notnull
        where T2 : notnull;
    public Task<List<AccountSummary>> GetAccountSummariesAsync(DateOnly asOf);
    public Task<List<DateOnly>> GetLastAvailableDatesAsync(int days);

}
