using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace Finance.Tracking.Services;

public class HistoryService : IHistoryService
{
    private FinanceDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryService"/> class.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public HistoryService(FinanceDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
        return await _dbContext.TotalMarketValues
            .Where(a => a.AsOf >= asOf)
            .AsNoTracking() // improves performance for read-only queries
            .ToListAsync();
    }


    public async Task<int> SaveTotalMarketValueAsync(DateOnly? asOf = null, double totalMarketValue = 0)
    {
        var actualDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var totalMarketValueEntry = await _dbContext.TotalMarketValues
            .FirstOrDefaultAsync(mv => mv.AsOf == actualDate && mv.Type == TotalMarketValueType.Total);

        if (totalMarketValueEntry != null)
        {
            totalMarketValueEntry.MarketValue = totalMarketValue;
            _dbContext.TotalMarketValues.Update(totalMarketValueEntry);
        }
        else
        {
            _dbContext.TotalMarketValues.Add(new TotalMarketValue
            {
                AsOf = actualDate,
                Type = TotalMarketValueType.Total,
                MarketValue = totalMarketValue
            });
        }
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<int> DeleteTotalMarketValueAsync(DateOnly? asOf = null)
    {
        var actualDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        int affectedRows = 0;

        // Check provider
        string? provider = _dbContext.Database.ProviderName;

        if (string.IsNullOrEmpty(provider))
        {
            throw new Exception("db provider not set");
        }
        if (provider.Contains("InMemory"))
        {
            // fallback for test providers
            var rows = _dbContext.TotalMarketValues.Where(mv => mv.AsOf == actualDate && mv.Type == TotalMarketValueType.Total);
            _dbContext.TotalMarketValues.RemoveRange(rows);
            affectedRows = await _dbContext.SaveChangesAsync();
        }
        else
        {
            // Directly delete matching rows in the database
            affectedRows = await _dbContext.TotalMarketValues
                .Where(mv => mv.AsOf == actualDate && mv.Type == TotalMarketValueType.Total)
                .ExecuteDeleteAsync();
        }
        return affectedRows; // number of rows deleted
    }
}