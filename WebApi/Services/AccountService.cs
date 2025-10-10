using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using Finance.Tracking.Models;

namespace Finance.Tracking.Services;


/// <summary>
/// AccountService refactored to use IValuationService and to ensure a single canonical
/// valuation path. Adds reconciliation logging and transactional persistence of summaries.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IPricingService _pricingService;
    private readonly IHistoryService _historyService;
    private readonly FinanceDbContext _dbContext;
    private readonly IValuationService _valuationService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IOptions<AccountOptions> options,
        IPricingService pricingService,
        IHistoryService historyService,
        FinanceDbContext dbContext,
        IValuationService valuationService,
        ILogger<AccountService> logger)
    {
        _pricingService = pricingService ?? throw new ArgumentNullException(nameof(pricingService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (options?.Value?.Accounts == null || !options.Value.Accounts.Any())
            throw new ArgumentException("No accounts configured", nameof(options));
    }

    /// <summary>
    /// Build and persist account summaries for a given date (default today).
    /// Uses the IValuationService as the single source-of-truth computation for values.
    /// </summary>
    public async Task BuildSummariesByDateAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var prices = await _pricingService.LoadPricesAsync(date);

        // Load existing summaries for this date keyed by composite PK (Name, Date, AssetClass)
        var existingSummaries = await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .ToDictionaryAsync(a => (a.Name, a.Date, a.AssetClass));

        var accounts = await _dbContext.Accounts
            .Include(a => a.Holdings)
            .ToListAsync();

        using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var account in accounts)
            {
                await BuildAndPersistSummariesForAccountAsync(account, prices, date, existingSummaries);
            }

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build summaries for date {Date}", date);
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Build/Upsert summaries for a single account and update account.MarketValue from persisted summaries.
    /// This ensures both BuildSummaries and UpdateAccount use the exact same persistence path.
    /// </summary>
    private async Task BuildAndPersistSummariesForAccountAsync(
        Account account,
        IReadOnlyDictionary<Symbol, double> prices,
        DateOnly date,
        Dictionary<(string Name, DateOnly Date, AssetClass AssetClass), AccountSummary> existingSummaries)
    {
        var summaries = new List<AccountSummary>();

        // Non-cash grouped by asset class
        var nonCashSummaries = account.Holdings
            .Where(h => h.Symbol != Symbol.CASH)
            .GroupBy(h => SymbolToAssetClass.Resolve(h.Symbol))
            .Select(g => new AccountSummary
            {
                Name = account.Name,
                Owner = account.Owner,
                Type = account.Type,
                AccountFilter = account.AccountFilter,
                Bank = account.Bank,
                Currency = account.Currency,
                Date = date,
                AssetClass = g.Key,
                MarketValue = g.Sum(h => _valuationService.ComputeHoldingValue(h, prices))
            })
            .ToList();

        summaries.AddRange(nonCashSummaries);

        // Cash - derive from holdings
        var cashHolding = account.Holdings.FirstOrDefault(h => h.Symbol == Symbol.CASH);
        if (cashHolding != null && cashHolding.Quantity > 0)
        {
            summaries.Add(new AccountSummary
            {
                Name = account.Name,
                Owner = account.Owner,
                Type = account.Type,
                AccountFilter = account.AccountFilter,
                Bank = account.Bank,
                Currency = account.Currency,
                Date = date,
                AssetClass = AssetClass.Cash,
                MarketValue = _valuationService.ComputeHoldingValue(cashHolding, prices)
            });
        }

        // Upsert summaries
        foreach (var summary in summaries)
        {
            var key = (summary.Name, summary.Date, summary.AssetClass);
            if (existingSummaries.TryGetValue(key, out var existing))
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(summary);
            }
            else
            {
                await _dbContext.AccountSummaries.AddAsync(summary);
                existingSummaries[key] = summary;
            }
        }

        // Recalculate & persist account total (single source of truth: sum of summaries)
        var total = summaries.Sum(s => s.MarketValue);
        account.MarketValue = total;
        _dbContext.Entry(account).Property(a => a.MarketValue).IsModified = true;

        // Reconciliation: compare computed total vs existing persisted sum (if any prior existed)
        double persistedTotal = await _dbContext.AccountSummaries
            .Where(s => s.Date == date && s.Name == account.Name)
            .SumAsync(s => (double?)s.MarketValue) ?? 0.0;

        if (Math.Abs(persistedTotal - total) > 0.0001)
        {
            _logger.LogWarning("Reconciliation mismatch for account {Account} on {Date}: computed={Computed}, persisted={Persisted}",
                account.Name, date, total, persistedTotal);
        }
    }

    private async Task<DateOnly?> GetLatestDateAvailableAsync()
        => await _dbContext.AccountSummaries
            .Select(a => a.Date)
            .OrderByDescending(d => d)
            .FirstOrDefaultAsync();

    public async Task<List<string>> GetAccountNamesAsync()
        => await _dbContext.Accounts.Select(a => a.Name).ToListAsync();

    public async Task<Account?> GetAccountByDateAsync(string accountName, DateOnly? date)
    {
        if (date is null)
            date = await GetLatestDateAvailableAsync();
        if (date is null)
            return null;

        var account = await _dbContext.Accounts
            .Include(a => a.Holdings)
            .FirstOrDefaultAsync(a => a.Name == accountName);

        if (account is null)
            return null;

        // Read authoritative persisted summaries (and optionally attach them to the account)
        var summaries = await _dbContext.AccountSummaries
            .Where(s => s.Name == accountName && s.Date == date)
            .AsNoTracking()
            .ToListAsync();

        account.MarketValue = summaries.Sum(s => s.MarketValue);
        return account;
    }

    public async Task CreateAccountAsync(Account account)
    {
        if (account is null) throw new ArgumentNullException(nameof(account));

        var exists = await _dbContext.Accounts.AnyAsync(a => a.Name == account.Name);
        if (exists)
            throw new InvalidOperationException($"Account {account.Name} already exists.");

        await _dbContext.Accounts.AddAsync(account);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Update an account and its holdings. After update, rebuild summaries for today via the canonical path.
    /// </summary>
    public async Task<Account?> UpdateAccountAsync(Account account)
    {
        if (account is null) throw new ArgumentNullException(nameof(account));

        // 1️⃣ Load existing account with holdings
        var existing = await _dbContext.Accounts
            .Include(a => a.Holdings)
            .FirstOrDefaultAsync(a => a.Name == account.Name);

        if (existing == null)
            return null;

        _dbContext.Entry(existing).CurrentValues.SetValues(account);

        var existingDict = existing.HoldingsDict; // Dictionary<Symbol, Holding>
        var updatedDict = account.HoldingsDict;   // Dictionary<Symbol, Holding>

        foreach (var kvp in updatedDict)
        {
            if (existingDict.TryGetValue(kvp.Key, out var existingHolding))
            {
                // Update quantity (or other scalar properties)
                existingHolding.Quantity = kvp.Value.Quantity;
            }
            else
            {
                // Add new holding
                kvp.Value.AccountName = existing.Name; // FK
                existing.Holdings.Add(kvp.Value);
            }
        }

        var toRemove = existing.Holdings
                            .Where(h => !updatedDict.ContainsKey(h.Symbol))
                            .ToList();
        foreach (var h in toRemove)
        {
            existing.Holdings.Remove(h);
            _dbContext.Holdings.Remove(h);
        }

        await _dbContext.SaveChangesAsync();

        var date = DateOnly.FromDateTime(DateTime.Today);
        var prices = await _pricingService.LoadPricesAsync(date);

        var existingSummaries = await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .ToDictionaryAsync(a => (a.Name, a.Date, a.AssetClass));

        await BuildAndPersistSummariesForAccountAsync(existing, prices, date, existingSummaries);

        await _dbContext.SaveChangesAsync();

        return await GetAccountByDateAsync(account.Name, null);
    }
    // Old implementation kept for reference
    public async Task<Account?> UpdateAccountAsyncOld(Account account)
    {
        if (account is null) throw new ArgumentNullException(nameof(account));

        // Load existing account including holdings
        var existing = await _dbContext.Accounts
            .Include(a => a.Holdings)
            .FirstOrDefaultAsync(a => a.Name == account.Name);

        if (existing == null)
            return null;

        // --- 1) Update scalar properties on Account ---
        existing.Owner = account.Owner;
        existing.Type = account.Type;
        existing.AccountFilter = account.AccountFilter;
        existing.Bank = account.Bank;
        existing.Currency = account.Currency;
        existing.MarketValue = account.MarketValue;

        // --- 2) Update holdings ---
        var existingDict = existing.Holdings.ToDictionary(h => h.Symbol);
        var updatedDict = account.Holdings.ToDictionary(h => h.Symbol);

        // Add or update holdings
        foreach (var kvp in updatedDict)
        {
            if (existingDict.TryGetValue(kvp.Key, out var existingHolding))
            {
                // Only update scalar properties
                existingHolding.Quantity = kvp.Value.Quantity;
                // Do NOT call SetValues — EF tracks this automatically
            }
            else
            {
                // New holding, attach with FK
                var newHolding = new Holding
                {
                    Symbol = kvp.Value.Symbol,
                    Quantity = kvp.Value.Quantity,
                    AccountName = existing.Name, // FK
                    Account = existing
                };
                existing.Holdings.Add(newHolding);
            }
        }

        // Remove holdings that are no longer present
        var toRemove = existing.Holdings
            .Where(h => !updatedDict.ContainsKey(h.Symbol))
            .ToList();

        foreach (var h in toRemove)
        {
            existing.Holdings.Remove(h);
            _dbContext.Holdings.Remove(h);
        }

        await _dbContext.SaveChangesAsync();

        // --- 3) Rebuild account summaries ---
        var date = DateOnly.FromDateTime(DateTime.Today);
        var prices = await _pricingService.LoadPricesAsync(date);

        // Fetch existing summaries for this date
        var existingSummaries = await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .ToDictionaryAsync(a => (a.Name, a.Date, a.AssetClass));

        await BuildAndPersistSummariesForAccountAsync(existing, prices, date, existingSummaries);
        await _dbContext.SaveChangesAsync();

        return existing;
    }

    public async Task<Dictionary<AssetClass, double>> GetTotalMarketValueByAssetClassAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        // Read authoritative persisted summaries (fast if indexed) rather than recomputing
        var grouped = await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .GroupBy(a => a.AssetClass)
            .Select(g => new { AssetClass = g.Key, Total = g.Sum(x => x.MarketValue) })
            .ToDictionaryAsync(x => x.AssetClass, x => x.Total);

        return grouped;
    }

    public async Task<Dictionary<GroupKey<string, AssetClass>, double>> GetTotalMarketValueByOwnerAndAssetClassAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        // Read persisted summaries and join to accounts to get owner
        var query = from s in _dbContext.AccountSummaries
                    join a in _dbContext.Accounts on s.Name equals a.Name
                    where s.Date == date
                    select new { a.Owner, s.AssetClass, s.MarketValue };

        var rows = await query.ToListAsync();

        return rows
            .GroupBy(x => new GroupKey<string, AssetClass> { Item1 = x.Owner, Item2 = x.AssetClass })
            .ToDictionary(g => g.Key, g => g.Sum(x => x.MarketValue));
    }

    public async Task<List<AccountSummary>> GetAccountSummariesAsync(DateOnly asOf)
        => await _dbContext.AccountSummaries
            .Where(a => a.Date == asOf)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<DateOnly>> GetLast30AvailableDatesAsync()
        => await _dbContext.AccountSummaries
            .Select(a => a.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(30)
            .ToListAsync();

    public async Task<List<DateOnly>> GetLastAvailableDatesAsync(int days)
        => await _dbContext.AccountSummaries
            .Select(a => a.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(days)
            .ToListAsync();

    public async Task<int> DeleteSummariesByDateAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        int deletedCount = 0;

        string? provider = _dbContext.Database.ProviderName;
        if (string.IsNullOrEmpty(provider))
            throw new Exception("db provider not set");

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

        int deletedHistory = await _historyService.DeleteTotalMarketValueAsync(date);
        return deletedCount;
    }

    public async Task<double> GetTotalMarketValueAsync(DateOnly asOf)
    {
        var total = await _dbContext.AccountSummaries
            .Where(a => a.Date == asOf)
            .AsNoTracking()
            .SumAsync(a => a.MarketValue);

        await _historyService.SaveTotalMarketValueAsync(asOf, total);
        return total;
    }

    public async Task<double> GetTotalMarketValueWherePredicateAsync(Expression<Func<AccountSummary, bool>> predicate, DateOnly asOf)
        => await _dbContext.AccountSummaries
            .Where(a => a.Date == asOf)
            .Where(predicate)
            .AsNoTracking()
            .SumAsync(a => a.MarketValue);

    public async Task<Dictionary<TKey, MarketValueGroup>> GetTotalMarketValueGroupedByWithNamesAsync<TKey>(
        Expression<Func<AccountSummary, TKey>> keySelector,
        DateOnly? asOf = null)
        where TKey : notnull
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        var summaries = await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .ToListAsync();

        return summaries
            .GroupBy(keySelector.Compile())
            .ToDictionary(
                g => g.Key,
                g => new MarketValueGroup
                {
                    Total = g.Sum(a => a.MarketValue),
                    AccountNames = g.Select(a => a.Name).Distinct().ToList()
                }
            );
    }

    public async Task<Dictionary<TKey, double>> GetTotalMarketValueGroupedByAsync<TKey>(Expression<Func<AccountSummary, TKey>> keySelector, DateOnly? asOf = null) where TKey : notnull
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        return await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .GroupBy(keySelector)
            .Select(g => new { g.Key, Total = g.Sum(a => a.MarketValue) })
            .ToDictionaryAsync(x => x.Key, x => x.Total);
    }

    public async Task<Dictionary<GroupKey<T1, T2>, double>> GetTotalMarketValueGroupedBy2Async<T1, T2>(
        Expression<Func<AccountSummary, T1>> key1,
        Expression<Func<AccountSummary, T2>> key2,
        DateOnly? asOf = null)
        where T1 : notnull
        where T2 : notnull
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        var rows = await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .ToListAsync();

        return rows
            .GroupBy(a => (Key1: key1.Compile()(a), Key2: key2.Compile()(a)))
            .Select(g => new { g.Key.Key1, g.Key.Key2, Total = g.Sum(a => a.MarketValue) })
            .ToDictionary(
                x => new GroupKey<T1, T2> { Item1 = x.Key1, Item2 = x.Key2 },
                x => x.Total
            );
    }
}

/// <summary>
/// Simple composite key helper used for grouping results of two keys.
/// </summary>
public class GroupKey<T1, T2> : IEquatable<GroupKey<T1, T2>>
    where T1 : notnull
    where T2 : notnull
{
    public T1 Item1 { get; set; } = default!;
    public T2 Item2 { get; set; } = default!;

    public bool Equals(GroupKey<T1, T2>? other) =>
        other != null && EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                        && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);

    public override bool Equals(object? obj) => Equals(obj as GroupKey<T1, T2>);
    public override int GetHashCode() => HashCode.Combine(Item1, Item2);
}
