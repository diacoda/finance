using System.Linq.Expressions;

namespace Finance.Tracking.Services;

public interface IHistoryService
{
    Task<int> SaveTotalMarketValueAsync(DateOnly? asOf = null, double totalMarketValue = 0);
    Task<List<TotalMarketValue>> GetHistoricalTotalMarkeValueAsync(int? days);
    Task<int> DeleteTotalMarketValueAsync(DateOnly? asOf = null);
}