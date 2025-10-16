namespace Finance.Tracking.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class TotalMarketValueRepository : ITotalMarketValueRepository
{
    private readonly FinanceDbContext _dbContext;

    public TotalMarketValueRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }
    public async Task<int> SaveTotalMarketValueAsync(DateOnly asOf, double totalMarketValue = 0)
    {
        var totalMarketValueEntry = await _dbContext.TotalMarketValues
            .FirstOrDefaultAsync(mv => mv.AsOf == asOf && mv.Type == TotalMarketValueType.Total);

        if (totalMarketValueEntry != null)
        {
            totalMarketValueEntry.MarketValue = totalMarketValue;
            _dbContext.TotalMarketValues.Update(totalMarketValueEntry);
        }
        else
        {
            _dbContext.TotalMarketValues.Add(new TotalMarketValue
            {
                AsOf = asOf,
                Type = TotalMarketValueType.Total,
                MarketValue = totalMarketValue
            });
        }
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<int> DeleteTotalMarketValueAsync(DateOnly asOf)
    {
        int affectedRows;
        string? provider = _dbContext.Database.ProviderName;

        if (string.IsNullOrEmpty(provider))
        {
            throw new Exception("db provider not set");
        }
        if (provider.Contains("InMemory"))
        {
            // fallback for test providers
            var rows = _dbContext.TotalMarketValues.Where(mv => mv.AsOf == asOf && mv.Type == TotalMarketValueType.Total);
            _dbContext.TotalMarketValues.RemoveRange(rows);
            affectedRows = await _dbContext.SaveChangesAsync();
        }
        else
        {
            // Directly delete matching rows in the database
            affectedRows = await _dbContext.TotalMarketValues
                .Where(mv => mv.AsOf == asOf && mv.Type == TotalMarketValueType.Total)
                .ExecuteDeleteAsync();
        }
        return affectedRows; // number of rows deleted

    }
    public async Task<List<TotalMarketValue>> GetHistoricalTotalMarkeValueAsync(DateOnly asOf)
    {
        return await _dbContext.TotalMarketValues
            .Where(a => a.AsOf >= asOf)
            .AsNoTracking() // improves performance for read-only queries
            .ToListAsync();
    }
}