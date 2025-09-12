using System.Linq.Expressions;
namespace Finance.Tracking.Interfaces;

public interface IPortfolioService
{
    Task<double> GetTotalMarketValueAsync(DateOnly? asOf = null);
    //Task<double> Total(Func<AccountSummary, bool> predicate, DateOnly? asOf = null);
    Task<double> GetTotalMarketValueWhereExpressionAsync(Expression<Func<AccountSummary, bool>> predicate, DateOnly? asOf = null);
    Task<double> GetTotalMarketValueByOwnerAsync(string owner, DateOnly? asOf = null);
    Task<Dictionary<string, double>> GetTotalMarketValueGroupedByOwnerAsync(DateOnly? asOf = null);
    Task<Dictionary<OwnerAccountFilterKey, double>> GetMarketValueByOwnerAndAccountFilterAsync(DateOnly? asOf = null);
    Task<Dictionary<OwnerTypeKey, double>> GetMarketValueByOwnerAndTypeAsync(DateOnly? asOf = null);
    //Task<Dictionary<OwnerTypeKey, double>> GetMarketValueByOwnerAndTypeAsync(DateOnly? asOf = null);

}