using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Finance.Tracking.Tests.Services;

public class AccountServiceTests
{
    private readonly FinanceDbContext _dbContext;
    private readonly Mock<IPricingService> _pricingServiceMock = new();
    private readonly Mock<IHistoryService> _historyServiceMock = new();
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        // In-memory EF Core DB
        var dbOptions = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new FinanceDbContext(dbOptions);

        // Configure dummy account options
        var accountOptions = new OptionsWrapper<AccountOptions>(
            new AccountOptions
            {
                Accounts = new Dictionary<string, AccountRaw>
                {
                    {
                        "Dan-TD-TFSA-USD",
                        new AccountRaw
                        {
                            Cash = 1000,
                            Holdings = new Dictionary<string, Holding>
                            {
                                { "VFV_TO", new Holding { Quantity = 5 } },
                                { "VCE_TO", new Holding { Quantity = 2 } }
                            }
                        }
                    },
                    {
                        "Oana-WS-RRSP-USD",
                        new AccountRaw
                        {
                            Cash = 5000,
                            Holdings = new Dictionary<string, Holding>()
                        }
                    }
                }
            });

        // Build AccountService with real options
        _accountService = new AccountService(
            accountOptions,
            _pricingServiceMock.Object,
            _historyServiceMock.Object,
            _dbContext
        );
    }

    [Fact]
    public async Task BuildSummariesByDateAsync_InsertsSummaries()
    {
        // Arrange
        var asOf = DateOnly.FromDateTime(DateTime.Today);

        _pricingServiceMock
            .Setup(p => p.LoadPricesAsync(asOf))
            .ReturnsAsync(new Dictionary<Symbol, double>
            {
                { Symbol.VFV_TO, 150.0 },
                { Symbol.VCE_TO, 300.0 }
            });

        // Act
        await _accountService.BuildSummariesByDateAsync(asOf);

        // Assert
        var summaries = await _dbContext.AccountSummaries.ToListAsync();
        Assert.Equal(2, summaries.Count);

        var danSummary = summaries.Find(s => s.Name == "Dan-TD-TFSA-USD");
        Assert.NotNull(danSummary);
        Assert.Equal(1000 + 5 * 150 + 2 * 300, danSummary!.MarketValue);

        var oanaSummary = summaries.Find(s => s.Name == "Oana-WS-RRSP-USD");
        Assert.NotNull(oanaSummary);
        Assert.Equal(5000, oanaSummary!.MarketValue);
    }

    [Fact]
    public async Task DeleteSummariesByDateAsync_DeletesExistingSummary()
    {
        // Arrange
        var asOf = DateOnly.FromDateTime(DateTime.Today);

        _dbContext.AccountSummaries.Add(new AccountSummary
        {
            Name = "Dan-TD-TFSA-USD",
            Owner = "Dan",         // required
            Bank = Bank.TD,        // required
            Type = AccountType.TFSA,
            AccountFilter = AccountFilter.TFSA,
            MarketValue = 1000,
            Date = asOf
        });
        await _dbContext.SaveChangesAsync();

        _historyServiceMock
            .Setup(h => h.DeleteTotalMarketValueAsync(asOf))
            .ReturnsAsync(1);

        // Act
        int deletedCount = await _accountService.DeleteSummariesByDateAsync(asOf);

        // Assert
        Assert.Equal(1, deletedCount); // should report one deleted
        Assert.Empty(_dbContext.AccountSummaries); // collection should be empty
        _historyServiceMock.Verify(h => h.DeleteTotalMarketValueAsync(asOf), Times.Once);
    }

}
