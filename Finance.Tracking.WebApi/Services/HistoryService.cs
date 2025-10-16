namespace Finance.Tracking.Services;

public class HistoryService : IHistoryService
{
    private ITotalMarketValueRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryService"/> class.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public HistoryService(ITotalMarketValueRepository totalMarketValueRepository)
    {
        _repository = totalMarketValueRepository ?? throw new ArgumentNullException(nameof(totalMarketValueRepository));
    }


    public async Task<List<TotalMarketValue>> GetHistoricalTotalMarkeValueAsync(int? days)
    {
        if (days.HasValue && days <= 0)
        {
            throw new ArgumentException("Days must be a positive integer.", nameof(days));
        }
        if (days > 30)
        {
            throw new ArgumentException("Days cannot exceed 30.", nameof(days));
        }

        DateOnly asOf = days.HasValue ? DateOnly.FromDateTime(DateTime.Today.AddDays(-days.Value)) : DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        return await _repository.GetHistoricalTotalMarkeValueAsync(asOf);
    }


    public async Task<int> SaveTotalMarketValueAsync(DateOnly? asOf = null, double totalMarketValue = 0)
    {
        var actualDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        return await _repository.SaveTotalMarketValueAsync(actualDate, totalMarketValue);
    }

    public async Task<int> DeleteTotalMarketValueAsync(DateOnly? asOf = null)
    {
        var actualDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        return await _repository.DeleteTotalMarketValueAsync(actualDate);
    }
}