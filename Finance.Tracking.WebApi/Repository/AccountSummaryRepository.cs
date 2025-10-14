using Finance.Tracking.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Finance.Tracking.Repository;

public class AccountSummaryRepository : IAccountSummaryRepository
{
    private readonly FinanceDbContext _dbContext;

    public AccountSummaryRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<DateOnly>> GetLast30AvailableDatesAsync()
            => await _dbContext.AccountSummaries
                .Select(a => a.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .Take(30)
                .ToListAsync();
    public async Task<Dictionary<(string Name, DateOnly Date, AssetClass AssetClass), AccountSummary>> GetSummariesByDateAsync(DateOnly date)
        => await _dbContext.AccountSummaries
            .Where(s => s.Date == date)
            .ToDictionaryAsync(s => (s.Name, s.Date, s.AssetClass));

    public async Task<List<AccountSummary>> GetSummariesForAccountAsync(string accountName, DateOnly date)
        => await _dbContext.AccountSummaries
            .Where(s => s.Name == accountName && s.Date == date)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<AccountSummary>> GetSummariesByDateRawAsync(DateOnly date)
        => await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .AsNoTracking()
            .ToListAsync();

    public async Task AddSummaryAsync(AccountSummary summary)
        => await _dbContext.AccountSummaries.AddAsync(summary);
    public async Task<DateOnly?> GetLatestDateAsync()
        => await _dbContext.AccountSummaries
            .Select(a => a.Date)
            .OrderByDescending(d => d)
            .FirstOrDefaultAsync();
    public async Task<int> DeleteSummariesByDateAsync(DateOnly date)
    {
        string? provider = _dbContext.Database.ProviderName;
        if (string.IsNullOrEmpty(provider))
            throw new Exception("db provider not set");

        int deletedCount;
        if (provider.Contains("InMemory"))
        {
            var summaries = _dbContext.AccountSummaries.Where(a => a.Date == date);
            _dbContext.AccountSummaries.RemoveRange(summaries);
            deletedCount = await _dbContext.SaveChangesAsync();
        }
        else
        {
            deletedCount = await _dbContext.AccountSummaries
                .Where(a => a.Date == date)
                .ExecuteDeleteAsync();
        }

        return deletedCount;
    }

    public async Task<double> GetTotalMarketValueAsync(DateOnly asOf)
        => await _dbContext.AccountSummaries
            .Where(a => a.Date == asOf)
            .AsNoTracking()
            .SumAsync(a => a.MarketValue);

    public async Task<double> GetTotalMarketValueWherePredicateAsync(Expression<Func<AccountSummary, bool>> predicate, DateOnly asOf)
        => await _dbContext.AccountSummaries
            .Where(a => a.Date == asOf)
            .Where(predicate)
            .AsNoTracking()
            .SumAsync(a => a.MarketValue);

    public async Task<Dictionary<AssetClass, double>> GetTotalMarketValueByAssetClassAsync(DateOnly asOf)
        => await _dbContext.AccountSummaries
            .Where(a => a.Date == asOf)
            .GroupBy(a => a.AssetClass)
            .Select(g => new { g.Key, Total = g.Sum(a => a.MarketValue) })
            .ToDictionaryAsync(x => x.Key, x => x.Total);

    public async Task<Dictionary<GroupKey<string, AssetClass>, double>> GetTotalMarketValueByOwnerAndAssetClassAsync(DateOnly asOf)
    {
        var query = from s in _dbContext.AccountSummaries
                    join a in _dbContext.Accounts on s.Name equals a.Name
                    where s.Date == asOf
                    select new { a.Owner, s.AssetClass, s.MarketValue };

        var rows = await query.ToListAsync();
        return rows
            .GroupBy(x => new GroupKey<string, AssetClass> { Item1 = x.Owner, Item2 = x.AssetClass })
            .ToDictionary(g => g.Key, g => g.Sum(x => x.MarketValue));
    }

    public async Task<Dictionary<TKey, MarketValueGroup>> GetTotalMarketValueGroupedByWithNamesAsync<TKey>(
        Expression<Func<AccountSummary, TKey>> keySelector,
        DateOnly asOf)
        where TKey : notnull
    {
        var summaries = await GetSummariesByDateRawAsync(asOf);
        return summaries
            .GroupBy(keySelector.Compile())
            .ToDictionary(
                g => g.Key,
                g => new MarketValueGroup
                {
                    Total = g.Sum(a => a.MarketValue),
                    AccountNames = g.Select(a => a.Name).Distinct().ToList()
                });
    }

    public async Task<Dictionary<TKey, double>> GetTotalMarketValueGroupedByAsync<TKey>(
        Expression<Func<AccountSummary, TKey>> keySelector,
        DateOnly asOf) where TKey : notnull
        => await _dbContext.AccountSummaries
            .Where(a => a.Date == asOf)
            .GroupBy(keySelector)
            .Select(g => new { g.Key, Total = g.Sum(a => a.MarketValue) })
            .ToDictionaryAsync(x => x.Key, x => x.Total);

    public async Task<Dictionary<GroupKey<T1, T2>, double>> GetTotalMarketValueGroupedBy2Async<T1, T2>(
        Expression<Func<AccountSummary, T1>> key1,
        Expression<Func<AccountSummary, T2>> key2,
        DateOnly asOf)
        where T1 : notnull
        where T2 : notnull
    {
        var summaries = await GetSummariesByDateRawAsync(asOf);
        return summaries
            .GroupBy(a => (Key1: key1.Compile()(a), Key2: key2.Compile()(a)))
            .Select(g => new { g.Key.Key1, g.Key.Key2, Total = g.Sum(a => a.MarketValue) })
            .ToDictionary(
                x => new GroupKey<T1, T2> { Item1 = x.Key1, Item2 = x.Key2 },
                x => x.Total
            );
    }

    public async Task<List<AccountSummary>> GetAccountSummariesAsync(DateOnly asOf)
        => await _dbContext.AccountSummaries
            .Where(a => a.Date == asOf)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<DateOnly>> GetLastAvailableDatesAsync(int days)
        => await _dbContext.AccountSummaries
            .Select(a => a.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(days)
            .ToListAsync();
}
