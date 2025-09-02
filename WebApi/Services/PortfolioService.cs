using System.Linq.Expressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Finance.Tracking.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IAccountService _accountService;

    public PortfolioService(IAccountService accountService)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
    }

    public async Task<double> GetTotalMarketValueAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        return await _accountService.GetTotalMarketValueAsync(date);
    }

    /*     public async Task<double> Total(Func<AccountSummary, bool> predicate, DateOnly? asOf = null)
        {
            var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
            var allSummaries = await _accountService.GetAccountSummariesAsync(date);
            return allSummaries.Where(predicate).Sum(a => a.MarketValue);
        } */

    public async Task<double> GetTotalMarketValueWhereExpressionAsync(Expression<Func<AccountSummary, bool>> predicate, DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        return await _accountService.GetTotalMarketValueWherePredicateAsync(predicate, date);
    }

    public async Task<Dictionary<TKey, double>> GroupBy_InMemory<TKey>(Func<AccountSummary, TKey> keySelector, DateOnly? asOf = null) where TKey : notnull
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var accounts = await _accountService.GetAccountSummariesAsync(date);

        return accounts
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.MarketValue));
    }

    public async Task<double> GetTotalMarketValueByOwnerAsync(string owner, DateOnly? asOf = null)
    {
        return await GetTotalMarketValueWhereExpressionAsync(a => a.Owner == owner, asOf);
    }

    public async Task<Dictionary<string, double>> GetTotalMarketValueGroupedByOwnerAsync(DateOnly? asOf = null)
    {
        // Use the generic method, passing the key selector for Owner
        return await _accountService.GetTotalMarketValueGroupedByAsync(a => a.Owner, asOf);
    }

    private async Task GroupByAccountFilterAndOwner()
    {
        var totalsByOwner = await _accountService.GetTotalMarketValueGroupedByAsync(a => a.Owner);

        // Example: iterate results
        foreach (var kvp in totalsByOwner)
        {
            Console.WriteLine($"Owner: {kvp.Key}, Total Market Value: {kvp.Value}");
        }

        var totalsByFilter = await _accountService.GetTotalMarketValueGroupedByAsync(a => a.AccountFilter);

        foreach (var kvp in totalsByFilter)
        {
            Console.WriteLine($"Filter: {kvp.Key}, Total Market Value: {kvp.Value}");
        }

        var totalsByOwnerAndType2 = await _accountService.GetTotalMarketValueGroupedByAsync(
            a => new { a.Owner, a.Type });

        foreach (var kvp in totalsByOwnerAndType2)
        {
            Console.WriteLine($"Owner: {kvp.Key.Owner}, Filter: {kvp.Key.Type}, Total: {kvp.Value}");
        }
    }

    /*
    var totalsByOwnerAndFilter = await accountService.GetMarketValueByOwnerAndAccountFilter();

    foreach (var kvp in totalsByOwnerAndFilter)
    {
        Console.WriteLine($"{kvp.Key.Owner} / {kvp.Key.AccountFilter}: {kvp.Value:C2}");
    }*/
    public async Task<Dictionary<OwnerAccountFilterKey, double>> GetMarketValueByOwnerAndAccountFilterAsync(DateOnly? asOf = null)
    {
        // Call the existing generic method using OwnerAccountFilterKey as TKey
        return await _accountService.GetTotalMarketValueGroupedByAsync(
            a => new OwnerAccountFilterKey(a.Owner, a.AccountFilter),
            asOf);
    }
    public async Task<Dictionary<OwnerTypeKey, double>> GetMarketValueByOwnerAndTypeAsync(DateOnly? asOf = null)
    {
        // Call the existing generic method using OwnerAccountFilterKey as TKey
        return await _accountService.GetTotalMarketValueGroupedByAsync(
            a => new OwnerTypeKey(a.Owner, a.Type),
            asOf);
    }

}
