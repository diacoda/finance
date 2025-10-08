using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using Finance.Tracking.Models;

namespace Finance.Tracking.Services;

public class AccountService : IAccountService
{
    //private Dictionary<string, Account> _accounts;
    private IPricingService _pricingService;
    private IHistoryService _historyService;
    private FinanceDbContext _dbContext;

    public AccountService(
        IOptions<AccountOptions> options,
        IPricingService pricingService,
        IHistoryService historyService,
        FinanceDbContext dbContext)
    {
        _pricingService = pricingService ?? throw new ArgumentNullException(nameof(pricingService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        if (options?.Value?.Accounts == null || !options.Value.Accounts.Any())
            throw new ArgumentException("No accounts configured", nameof(options));

        /*
        _accounts = new Dictionary<string, Account>();

        foreach (var (accountName, raw) in options.Value.Accounts)
        {
            var parts = accountName.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
                throw new ArgumentException($"Invalid account key: {accountName}");

            var parsedType = Enum.Parse<AccountType>(parts[2], true);

            var account = new Account
            {
                Owner = parts[0],
                Bank = Enum.Parse<Bank>(parts[1], true),
                Type = parsedType,
                AccountFilter = parsedType.ToAccountFilter(),
                MarketValue = 0,
                Currency = Enum.Parse<Currency>(parts[3], true),
                Name = accountName,
                Cash = raw.Cash
            };

            foreach (var (symbolKey, holding) in raw.Holdings)
            {
                var normalized = symbolKey.ToString().Replace('.', '_');
                if (Enum.TryParse<Symbol>(normalized, true, out var symbol))
                    account.Holdings.Add(new Holding() { Symbol = symbol, Quantity = holding.Quantity });
            }
            _accounts[accountName] = account;
        }
        */
    }

    /*
    public async Task InitializeAsync()
    {
        // Get all account names that already exist in the database
        var existingAccountNames = await _dbContext.Accounts
                                            .Where(a => _accounts.Keys.Contains(a.Name))
                                            .Select(a => a.Name)
                                            .ToListAsync();

        // Filter only accounts that are missing
        var accountsToCreate = _accounts
            .Where(kvp => !existingAccountNames.Contains(kvp.Key))
            .Select(kvp => kvp.Value);

        // Add missing accounts along with their holdings
        if (accountsToCreate.Any())
        {
            await _dbContext.Accounts.AddRangeAsync(accountsToCreate);
            await _dbContext.SaveChangesAsync();
        }
    }
    */

    public async Task BuildSummariesByDateAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var prices = await _pricingService.LoadPricesAsync(date);

        // ✅ Use full composite PK (Name, Date, AssetClass)
        var existingSummaries = await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .ToDictionaryAsync(a => (a.Name, a.Date, a.AssetClass));

        //foreach (var account in _accounts.Values)
        //foreach (var account in _dbContext.Accounts)
        foreach (var account in await _dbContext.Accounts.Include(a => a.Holdings).ToListAsync())
        {
            var summaries = BuildAccountSummaries(account, prices, date);

            foreach (var summary in summaries)
            {
                var key = (summary.Name, summary.Date, summary.AssetClass);

                if (existingSummaries.TryGetValue(key, out var existing))
                {
                    // ✅ Update existing tracked entity
                    _dbContext.Entry(existing).CurrentValues.SetValues(summary);
                }
                else
                {
                    // ✅ Add new one
                    await _dbContext.AccountSummaries.AddAsync(summary);
                }
            }
            // ✅ Recalculate & persist account total
            //_dbContext.Entry(account).Property(a => a.MarketValue).IsModified = true;
        }
        await _dbContext.SaveChangesAsync();
    }

    private IEnumerable<AccountSummary> BuildAccountSummaries(
        Account account,
        IReadOnlyDictionary<Symbol, double> prices,
        DateOnly date)
    {
        var summaries = new List<AccountSummary>();

        var grouped = GroupByAssetClass(account, prices, date).ToList();
        summaries.AddRange(grouped);

        if (account.Cash > 0)
            summaries.Add(AddCashSummary(account, date));

        // (optional) total summary for debugging
        double totalValue = grouped.Sum(s => s.MarketValue) + account.Cash;
        account.MarketValue = totalValue;

        return summaries;
    }

    private IEnumerable<AccountSummary> GroupByAssetClass(
        Account account,
        IReadOnlyDictionary<Symbol, double> prices,
        DateOnly date)
    {
        return account.Holdings
            .GroupBy(h => SymbolToAssetClass.Resolve(h.Symbol))
            .Select(g => new AccountSummary
            {
                Name = account.Name,
                Owner = account.Owner,
                Type = account.Type,
                AccountFilter = account.AccountFilter,
                Bank = account.Bank,
                Currency = account.Currency,
                Cash = 0, // ✅ don’t repeat cash here
                Date = date,
                AssetClass = g.Key,
                MarketValue = g.Sum(h =>
                    prices.TryGetValue(h.Symbol, out var price)
                        ? h.Quantity * price
                        : 0.0)
            });
    }

    private AccountSummary AddCashSummary(Account account, DateOnly date)
    {
        AccountSummary cashSummary = new AccountSummary
        {
            Name = account.Name,
            Owner = account.Owner,
            Type = account.Type,
            AccountFilter = account.AccountFilter,
            Bank = account.Bank,
            Currency = account.Currency,
            Cash = account.Cash,
            MarketValue = account.Cash,
            Date = date,
            AssetClass = AssetClass.Cash
        };
        return cashSummary;
    }

    private async Task<DateOnly?> GetLatestDateAvailableAsync()
    => await _dbContext.AccountSummaries
        .Select(a => a.Date)
        .OrderByDescending(d => d)
        .FirstOrDefaultAsync();

    public List<string> GetAccountNames()
    {
        //return new List<string>(_accounts.Keys);
        return new List<string>(_dbContext.Accounts.Select(a => a.Name));
    }

    // Get an account by its name
    public async Task<Account?> GetAccountByDateAsync(string accountName, DateOnly? date)
    {
        if (date is null)
            date = await GetLatestDateAvailableAsync();
        if (date is null)
            return null;

        Account? account = await _dbContext.Accounts
            .Include(a => a.Holdings)
            .FirstOrDefaultAsync(a => a.Name == accountName);
        if (account is null)
            return null;

        // ✅ Aggregate all summaries for this account & date
        double totalMarketValue = await _dbContext.AccountSummaries
            .Where(s => s.Name == accountName && s.Date == date)
            .SumAsync(s => (double?)s.MarketValue) ?? 0.0;

        account.MarketValue = totalMarketValue;

        return account;
    }


    public async Task CreateAccountAsync(Account account)
    {
        // Optional: check if account already exists
        var exists = await _dbContext.Accounts
                                .AnyAsync(a => a.Name == account.Name);
        if (exists)
        {
            throw new InvalidOperationException($"Account {account.Name} already exists.");
        }

        // Add the account along with its holdings
        await _dbContext.Accounts.AddAsync(account);

        // Save changes to persist both account and holdings
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAccountAsync(Account account)
    {
        // Load existing account with holdings
        var existing = await _dbContext.Accounts
                                    .Include(a => a.Holdings)
                                    .FirstOrDefaultAsync(a => a.Name == account.Name);
        if (existing != null)
        {
            // Update scalar properties
            _dbContext.Entry(existing).CurrentValues.SetValues(account);

            // Build dictionaries for fast lookup
            var existingDict = existing.HoldingsDict; // Dictionary<Symbol, Holding>
            var updatedDict = account.HoldingsDict;   // Dictionary<Symbol, Holding>

            // 1. Update existing holdings or add new ones
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
                    existing.Holdings.Add(kvp.Value);
                }
            }

            // 2. Remove holdings that no longer exist
            var toRemove = existing.Holdings
                                .Where(h => !updatedDict.ContainsKey(h.Symbol))
                                .ToList();
            foreach (var h in toRemove)
            {
                existing.Holdings.Remove(h);
                _dbContext.Holdings.Remove(h);
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Dictionary<AssetClass, double>> GetTotalMarketValueByAssetClassAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var prices = await _pricingService.LoadPricesAsync(date);

        // Load accounts with holdings
        var accounts = await _dbContext.Accounts
            .Include(a => a.Holdings)
            .ToListAsync();

        var values = accounts
            // holdings
            .SelectMany(a => a.Holdings.Select(h => new
            {
                AssetClass = SymbolToAssetClass.Resolve(h.Symbol),
                Value = prices.TryGetValue(h.Symbol, out var p) ? h.Quantity * p : 0
            }))
            // add cash
            .Concat(accounts.Select(a => new
            {
                AssetClass = AssetClass.Cash,
                Value = a.Cash
            }))
            .GroupBy(x => x.AssetClass)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Value)
            );

        return values;
    }

    public async Task<Dictionary<GroupKey<string, AssetClass>, double>> GetTotalMarketValueByOwnerAndAssetClassAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var prices = await _pricingService.LoadPricesAsync(date);

        // Load accounts with holdings
        var accounts = await _dbContext.Accounts
            .Include(a => a.Holdings)
            .ToListAsync();

        var values = accounts
            // holdings
            .SelectMany(a => a.Holdings.Select(h => new
            {
                Owner = a.Owner,
                AssetClass = SymbolToAssetClass.Resolve(h.Symbol),
                Value = prices.TryGetValue(h.Symbol, out var p) ? h.Quantity * p : 0
            }))
            // add cash
            .Concat(accounts.Select(a => new
            {
                Owner = a.Owner,
                AssetClass = AssetClass.Cash,
                Value = a.Cash
            }))
            .GroupBy(x => new GroupKey<string, AssetClass>
            {
                Item1 = x.Owner,
                Item2 = x.AssetClass
            })
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Value)
            );

        return values;
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
        // Check provider
        string? provider = _dbContext.Database.ProviderName;

        if (string.IsNullOrEmpty(provider))
        {
            throw new Exception("db provider not set");
        }
        if (provider.Contains("InMemory"))
        {
            // fallback for test providers
            var summaries = _dbContext.AccountSummaries.Where(a => a.Date == date);
            _dbContext.AccountSummaries.RemoveRange(summaries);
            deletedCount = await _dbContext.SaveChangesAsync();
        }
        else
        {
            // production (SQL Server, PostgreSQL, etc.)
            deletedCount = await _dbContext.AccountSummaries
                .Where(a => a.Date == date)
                .ExecuteDeleteAsync();
        }
        int deleted = await _historyService.DeleteTotalMarketValueAsync(date);
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

        // Load the rows for the given date into memory
        var summaries = await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .ToListAsync();

        // Group and compute totals + account names
        return summaries
            .GroupBy(keySelector.Compile()) // compile expression to delegate
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

        return await _dbContext.AccountSummaries
            .Where(a => a.Date == date)
            .GroupBy(a => new { Key1 = key1.Compile()(a), Key2 = key2.Compile()(a) }) // optional: map keys in memory if needed
            .Select(g => new { g.Key.Key1, g.Key.Key2, Total = g.Sum(a => a.MarketValue) })
            .ToDictionaryAsync(x => new GroupKey<T1, T2> { Item1 = x.Key1, Item2 = x.Key2 }, x => x.Total);
    }
}

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
