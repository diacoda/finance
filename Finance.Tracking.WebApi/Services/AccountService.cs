using Microsoft.Extensions.Options;
using Finance.Tracking.Models;
using Finance.Tracking.Repository;
using System.Linq.Expressions;

namespace Finance.Tracking.Services;

/// <summary>
/// AccountService fully repository-driven, uses IValuationService and IAccountSummaryRepository
/// for canonical valuations and persistence. Adds reconciliation logging and transactional operations.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IPricingService _pricingService;
    private readonly IHistoryService _historyService;
    private readonly IValuationService _valuationService;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountSummaryRepository _accountSummaryRepository;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IOptions<AccountOptions> options,
        IPricingService pricingService,
        IHistoryService historyService,
        IValuationService valuationService,
        IAccountRepository accountRepository,
        IAccountSummaryRepository accountSummaryRepository,
        ILogger<AccountService> logger)
    {
        _pricingService = pricingService ?? throw new ArgumentNullException(nameof(pricingService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _accountSummaryRepository = accountSummaryRepository ?? throw new ArgumentNullException(nameof(accountSummaryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (options?.Value?.Accounts == null || !options.Value.Accounts.Any())
            throw new ArgumentException("No accounts configured", nameof(options));
    }

    // -------------------------------
    // Core Account CRUD Operations
    // -------------------------------

    public async Task<List<string>> GetAccountNamesAsync()
        => await _accountRepository.GetAccountNamesAsync();

    public async Task<Account?> GetAccountByDateAsync(string accountName, DateOnly? date)
    {
        date ??= await _accountSummaryRepository.GetLatestDateAsync();
        if (date == null) return null;

        var account = await _accountRepository.GetAccountWithHoldingsAsync(accountName);
        if (account == null) return null;

        var summaries = await _accountSummaryRepository.GetSummariesForAccountAsync(accountName, date.Value);
        account.MarketValue = summaries.Sum(s => s.MarketValue);

        return account;
    }

    public async Task CreateAccountAsync(Account account)
    {
        if (account == null) throw new ArgumentNullException(nameof(account));
        await _accountRepository.CreateAccountAsync(account);
    }

    public async Task<Account?> UpdateAccountAsync(Account account)
    {
        if (account == null) throw new ArgumentNullException(nameof(account));

        var existing = await _accountRepository.GetAccountWithHoldingsAsync(account.Name);
        if (existing == null) return null;

        await _accountRepository.UpdateAccount(existing, account);

        DateOnly today = DateOnly.FromDateTime(DateTime.Today); // needs to be optimized not full day.
        await _accountSummaryRepository.DeleteSummariesByDateAsync(today);

        await BuildSummariesByDateAsync(today);

        return await GetAccountByDateAsync(account.Name, today);
    }

    // -------------------------------
    // Build / Reconciliation Operations
    // -------------------------------

    public async Task BuildSummariesByDateAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var prices = await _pricingService.LoadPricesAsync(date);
        var existingSummaries = await _accountSummaryRepository.GetSummariesByDateAsync(date);
        var accounts = await _accountRepository.GetAllAccountsWithHoldingsAsync();

        using var tx = await _accountRepository.BeginTransactionAsync();
        try
        {
            foreach (var account in accounts)
            {
                await BuildAndPersistSummariesForAccountAsync(account, prices, date, existingSummaries);
            }

            await _accountRepository.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build summaries for date {Date}", date);
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task BuildAndPersistSummariesForAccountAsync(
        Account account,
        IReadOnlyDictionary<Symbol, double> prices,
        DateOnly date,
        Dictionary<(string Name, DateOnly Date, AssetClass AssetClass), AccountSummary> existingSummaries)
    {
        var summaries = new List<AccountSummary>();

        // Non-cash asset summaries
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

        // Cash summary
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
                existing.MarketValue = summary.MarketValue;
            }
            else
            {
                await _accountSummaryRepository.AddSummaryAsync(summary);
                existingSummaries[key] = summary;
            }
        }

        // Reconciliation
        account.MarketValue = summaries.Sum(s => s.MarketValue);
        var persistedTotal = (await _accountSummaryRepository.GetSummariesForAccountAsync(account.Name, date))
            .Sum(s => s.MarketValue);

        if (Math.Abs(persistedTotal - account.MarketValue) > 0.0001)
        {
            _logger.LogWarning(
                "Reconciliation mismatch for account {Account} on {Date}: computed={Computed}, persisted={Persisted}",
                account.Name, date, account.MarketValue, persistedTotal);
        }
    }

    // -------------------------------
    // Market Value Queries
    // -------------------------------

    public async Task<double> GetTotalMarketValueAsync(DateOnly asOf)
    {
        var total = await _accountSummaryRepository.GetTotalMarketValueAsync(asOf);
        await _historyService.SaveTotalMarketValueAsync(asOf, total);
        return total;
    }

    public async Task<double> GetTotalMarketValueWherePredicateAsync(Expression<Func<AccountSummary, bool>> predicate, DateOnly asOf)
        => await _accountSummaryRepository.GetTotalMarketValueWherePredicateAsync(predicate, asOf);

    public async Task<Dictionary<AssetClass, double>> GetTotalMarketValueByAssetClassAsync(DateOnly? asOf = null)
        => await _accountSummaryRepository.GetTotalMarketValueByAssetClassAsync(asOf ?? DateOnly.FromDateTime(DateTime.Today));

    public async Task<Dictionary<GroupKey<string, AssetClass>, double>> GetTotalMarketValueByOwnerAndAssetClassAsync(DateOnly? asOf = null)
        => await _accountSummaryRepository.GetTotalMarketValueByOwnerAndAssetClassAsync(asOf ?? DateOnly.FromDateTime(DateTime.Today));

    public async Task<Dictionary<TKey, MarketValueGroup>> GetTotalMarketValueGroupedByWithNamesAsync<TKey>(
        Expression<Func<AccountSummary, TKey>> keySelector,
        DateOnly? asOf = null) where TKey : notnull
        => await _accountSummaryRepository.GetTotalMarketValueGroupedByWithNamesAsync(keySelector, asOf ?? DateOnly.FromDateTime(DateTime.Today));

    public async Task<Dictionary<TKey, double>> GetTotalMarketValueGroupedByAsync<TKey>(
        Expression<Func<AccountSummary, TKey>> keySelector,
        DateOnly? asOf = null) where TKey : notnull
        => await _accountSummaryRepository.GetTotalMarketValueGroupedByAsync(keySelector, asOf ?? DateOnly.FromDateTime(DateTime.Today));

    public async Task<Dictionary<GroupKey<T1, T2>, double>> GetTotalMarketValueGroupedBy2Async<T1, T2>(
        Expression<Func<AccountSummary, T1>> key1,
        Expression<Func<AccountSummary, T2>> key2,
        DateOnly? asOf = null) where T1 : notnull where T2 : notnull
        => await _accountSummaryRepository.GetTotalMarketValueGroupedBy2Async(key1, key2, asOf ?? DateOnly.FromDateTime(DateTime.Today));

    // -------------------------------
    // Delete / Summaries Operations
    // -------------------------------

    public async Task<int> DeleteSummariesByDateAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        int deletedCount = await _accountSummaryRepository.DeleteSummariesByDateAsync(date);
        await _historyService.DeleteTotalMarketValueAsync(date);
        return deletedCount;
    }

    public async Task<List<AccountSummary>> GetAccountSummariesAsync(DateOnly asOf)
        => await _accountSummaryRepository.GetAccountSummariesAsync(asOf);

    // -------------------------------
    // Helper / Misc
    // -------------------------------

    public async Task<List<DateOnly>> GetLast30AvailableDatesAsync()
        => await _accountSummaryRepository.GetLast30AvailableDatesAsync();

    public async Task<List<DateOnly>> GetLastAvailableDatesAsync(int days)
        => await _accountSummaryRepository.GetLastAvailableDatesAsync(days);
}
