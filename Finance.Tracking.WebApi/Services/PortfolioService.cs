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

    private async Task<Dictionary<AssetClass, double>> GetTotalMarketValueGroupedByAssetClassAsync(DateOnly? asOf = null)
    {
        return await _accountService.GetTotalMarketValueByAssetClassAsync(asOf);
    }

    public async Task<List<AssetClassTotalDTO>> GetTotalMarketValueGroupedByAssetClassWithPercentageAsync(DateOnly? asOf = null)
    {
        var totals = await GetTotalMarketValueGroupedByAssetClassAsync(asOf);
        var grandTotal = totals.Values.Sum();

        if (grandTotal <= 0)
            return new List<AssetClassTotalDTO>();

        var breakdown = totals.Select(kvp => new AssetClassTotalDTO
        {
            AssetClass = kvp.Key.ToString(),
            Total = kvp.Value,
            Percentage = (kvp.Value / grandTotal) * 100
        }).ToList();

        // normalize rounding drift
        double totalPct = breakdown.Sum(b => b.Percentage);
        double drift = 100 - totalPct;
        if (Math.Abs(drift) > 0.0001)
        {
            var maxItem = breakdown.OrderByDescending(b => b.Percentage).First();
            maxItem.Percentage += drift;
        }

        return breakdown;
    }

    private async Task<Dictionary<GroupKey<string, AssetClass>, double>> GetTotalMarketValueGroupedByOwnerAndAssetClassAsync(DateOnly? asOf = null)
    {
        return await _accountService.GetTotalMarketValueByOwnerAndAssetClassAsync(asOf);
    }

    public async Task<List<OwnerAssetClassTotalDTO>> GetTotalMarketValueGroupedByOwnerAndAssetClassWithPercentageAsync(DateOnly? asOf = null)
    {
        var totals = await GetTotalMarketValueGroupedByOwnerAndAssetClassAsync(asOf);

        // group by owner to compute percentages relative to each owner
        var result = new List<OwnerAssetClassTotalDTO>();
        var groupedByOwner = totals.GroupBy(kvp => kvp.Key.Item1);

        foreach (var ownerGroup in groupedByOwner)
        {
            double grandTotal = ownerGroup.Sum(x => x.Value);
            if (grandTotal <= 0) continue;

            var ownerBreakdown = ownerGroup.Select(kvp => new OwnerAssetClassTotalDTO
            {
                Owner = kvp.Key.Item1,
                AssetClass = kvp.Key.Item2.ToString(),
                Total = kvp.Value,
                Percentage = (kvp.Value / grandTotal) * 100
            }).ToList();

            // normalize rounding drift to ensure exactly 100% per owner
            double totalPct = ownerBreakdown.Sum(b => b.Percentage);
            double drift = 100 - totalPct;
            if (Math.Abs(drift) > 0.0001)
            {
                var maxItem = ownerBreakdown.OrderByDescending(b => b.Percentage).First();
                maxItem.Percentage += drift;
            }

            result.AddRange(ownerBreakdown);
        }

        return result;
    }

    public async Task<double> GetTotalMarketValueAsync(DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        return await _accountService.GetTotalMarketValueAsync(date);
    }

    public async Task<double> GetTotalMarketValueWhereExpressionAsync(Expression<Func<AccountSummary, bool>> predicate, DateOnly? asOf = null)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        return await _accountService.GetTotalMarketValueWherePredicateAsync(predicate, date);
    }

    private async Task<Dictionary<TKey, double>> GroupBy_InMemory<TKey>(Func<AccountSummary, TKey> keySelector, DateOnly? asOf = null) where TKey : notnull
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
