namespace Finance.Tracking.Infrastructure.Repositories;

public interface ITotalMarketValueRepository
{
    public Task<int> SaveTotalMarketValueAsync(DateOnly asOf, double totalMarketValue = 0);
    public Task<int> DeleteTotalMarketValueAsync(DateOnly asOf);
    public Task<List<TotalMarketValue>> GetHistoricalTotalMarkeValueAsync(DateOnly asOf);
}