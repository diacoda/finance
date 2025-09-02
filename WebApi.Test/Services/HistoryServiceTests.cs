using Microsoft.EntityFrameworkCore;

namespace Finance.Tracking.Tests.Services;

public class HistoryServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
            .Options;

        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task GetHistoricalTotalMarkeValueAsync_ReturnsExpectedData()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var service = new HistoryService(dbContext);

        var today = DateOnly.FromDateTime(DateTime.Today);
        dbContext.TotalMarketValues.AddRange(
            new TotalMarketValue { AsOf = today, Type = TotalMarketValueType.Total, MarketValue = 100 },
            new TotalMarketValue { AsOf = today.AddDays(-5), Type = TotalMarketValueType.Total, MarketValue = 200 }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.GetHistoricalTotalMarkeValueAsync(7);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetHistoricalTotalMarkeValueAsync_InvalidDays_Throws(int days)
    {
        using var dbContext = GetInMemoryDbContext();
        var service = new HistoryService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetHistoricalTotalMarkeValueAsync(days));
    }

    [Fact]
    public async Task SaveTotalMarketValueAsync_AddsNewEntry_WhenNotExists()
    {
        using var dbContext = GetInMemoryDbContext();
        var service = new HistoryService(dbContext);

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var result = await service.SaveTotalMarketValueAsync(today, 500);

        // Assert
        Assert.Equal(1, result); // one insert
        var entry = await dbContext.TotalMarketValues.FirstOrDefaultAsync(mv => mv.AsOf == today);
        Assert.NotNull(entry);
        Assert.Equal(500, entry.MarketValue);
    }

    [Fact]
    public async Task SaveTotalMarketValueAsync_UpdatesEntry_WhenExists()
    {
        using var dbContext = GetInMemoryDbContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        dbContext.TotalMarketValues.Add(new TotalMarketValue
        {
            AsOf = today,
            Type = TotalMarketValueType.Total,
            MarketValue = 100
        });
        await dbContext.SaveChangesAsync();

        var service = new HistoryService(dbContext);

        // Act
        var result = await service.SaveTotalMarketValueAsync(today, 999);

        // Assert
        Assert.Equal(1, result); // one update
        var entry = await dbContext.TotalMarketValues.FirstOrDefaultAsync(mv => mv.AsOf == today);
        Assert.NotNull(entry);
        Assert.Equal(999, entry.MarketValue);
    }

    [Fact]
    public async Task DeleteTotalMarketValueAsync_RemovesEntry()
    {
        using var dbContext = GetInMemoryDbContext();
        var today = DateOnly.FromDateTime(DateTime.Today);

        dbContext.TotalMarketValues.Add(new TotalMarketValue
        {
            AsOf = today,
            Type = TotalMarketValueType.Total,
            MarketValue = 123
        });
        await dbContext.SaveChangesAsync();

        var service = new HistoryService(dbContext);

        // Act
        var deleted = await service.DeleteTotalMarketValueAsync(today);

        // Assert
        Assert.Equal(1, deleted); // one row deleted
        Assert.False(dbContext.TotalMarketValues.Any(mv => mv.AsOf == today));
    }
}
