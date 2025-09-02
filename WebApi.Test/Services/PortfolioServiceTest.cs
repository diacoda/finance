using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Finance.Tracking.Models;
using Finance.Tracking.Services;
using Moq;
using Xunit;

namespace Finance.Tracking.Tests.Services;

public class PortfolioServiceTests
{
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly PortfolioService _portfolioService;

    public PortfolioServiceTests()
    {
        _accountServiceMock = new Mock<IAccountService>();
        _portfolioService = new PortfolioService(_accountServiceMock.Object);
    }


    [Fact]
    public async Task GetTotalMarketValueAsync_ReturnsValueFromAccountService()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        _accountServiceMock
            .Setup(s => s.GetTotalMarketValueAsync(today))
            .ReturnsAsync(1234.56);

        // Act
        var result = await _portfolioService.GetTotalMarketValueAsync();

        // Assert
        Assert.Equal(1234.56, result);
    }

    [Fact]
    public async Task GetTotalMarketValueWhereExpressionAsync_ReturnsCorrectValue()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);

        _accountServiceMock
            .Setup(s => s.GetTotalMarketValueWherePredicateAsync(
                It.IsAny<Expression<Func<AccountSummary, bool>>>(),
                today))
            .ReturnsAsync(5000);

        // Act
        var result = await _portfolioService.GetTotalMarketValueWhereExpressionAsync(a => a.Owner == "Dan");

        // Assert
        Assert.Equal(5000, result);
    }

    [Fact]
    public async Task GroupBy_InMemory_GroupsCorrectly()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);

        var accounts = new List<AccountSummary>
            {
                                new() {
                    Name="Dan-TD-TFSA-USD",
                    Owner = "Dan",
                    Type = AccountType.TFSA,
                    AccountFilter = AccountFilter.TFSA,
                    Bank = Bank.TD,
                    Cash = 0,
                    Currency = Currency.USD,
                    MarketValue = 100,
                    Date = today
                },
                new() {
                    Name="Dan-TD-RRSP-USD",
                    Owner = "Dan",
                    Type =AccountType.RRSP,
                    AccountFilter = AccountFilter.RRSP,
                    Bank = Bank.TD,
                    Cash = 0,
                    Currency = Currency.USD,
                    MarketValue = 200,
                    Date = today },
                new() {
                    Name = "Oana-WS-TFSA-USD",
                    Owner = "Oana",
                    Type = AccountType.TFSA,
                    AccountFilter = AccountFilter.TFSA,
                    Bank = Bank.WS,
                    Cash = 0,
                    Currency = Currency.USD,
                    MarketValue = 300,
                    Date = today }
            };

        _accountServiceMock
            .Setup(s => s.GetAccountSummariesAsync(today))
            .ReturnsAsync(accounts);

        // Act
        var result = await _portfolioService.GroupBy_InMemory(a => a.Owner);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(300, result["Dan"]);
        Assert.Equal(300, result["Oana"]);
    }

    [Fact]
    public async Task GetTotalMarketValueByOwnerAsync_UsesPredicate()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);

        _accountServiceMock
            .Setup(s => s.GetTotalMarketValueWherePredicateAsync(
                It.IsAny<Expression<Func<AccountSummary, bool>>>(),
                today))
            .ReturnsAsync(1500);

        // Act
        var result = await _portfolioService.GetTotalMarketValueByOwnerAsync("Dan");

        // Assert
        Assert.Equal(1500, result);
    }

    [Fact]
    public async Task GetTotalMarketValueGroupedByOwnerAsync_ReturnsExpectedDict()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expected = new Dictionary<string, double>
            {
                { "Dan", 1000 },
                { "Oana", 2000 }
            };

        _accountServiceMock
            .Setup(s => s.GetTotalMarketValueGroupedByAsync(
                It.IsAny<Expression<Func<AccountSummary, string>>>(),
                It.IsAny<DateOnly?>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _portfolioService.GetTotalMarketValueGroupedByOwnerAsync(today);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetMarketValueByOwnerAndAccountFilterAsync_ReturnsExpectedDict()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expected = new Dictionary<OwnerAccountFilterKey, double>
            {
                { new OwnerAccountFilterKey("Dan", AccountFilter.RRSP), 5000 },
                { new OwnerAccountFilterKey("Oana", AccountFilter.TFSA), 7000 }
            };

        _accountServiceMock
            .Setup(s => s.GetTotalMarketValueGroupedByAsync(
                It.IsAny<Expression<Func<AccountSummary, OwnerAccountFilterKey>>>(),
                It.IsAny<DateOnly?>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _portfolioService.GetMarketValueByOwnerAndAccountFilterAsync(today);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetMarketValueByOwnerAndTypeAsync_ReturnsExpectedDict()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expected = new Dictionary<OwnerTypeKey, double>
            {
                { new OwnerTypeKey("Dan", AccountType.RRSP), 1000 },
                { new OwnerTypeKey("Oana", AccountType.TFSA), 3000 }
            };

        _accountServiceMock
            .Setup(s => s.GetTotalMarketValueGroupedByAsync(
                It.IsAny<Expression<Func<AccountSummary, OwnerTypeKey>>>(),
                It.IsAny<DateOnly?>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _portfolioService.GetMarketValueByOwnerAndTypeAsync(today);

        // Assert
        Assert.Equal(expected, result);
    }


    [Fact]
    public async Task GetTotalMarketValueAsync_ReturnsValue_FromAccountService()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        _accountServiceMock
            .Setup(a => a.GetTotalMarketValueAsync(today))
            .ReturnsAsync(1000);

        // Act
        var result = await _portfolioService.GetTotalMarketValueAsync();

        // Assert
        Assert.Equal(1000, result);
        _accountServiceMock.Verify(a => a.GetTotalMarketValueAsync(today), Times.Once);
    }

    [Fact]
    public async Task GetTotalMarketValueWhereExpressionAsync_ReturnsFilteredValue()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        Expression<Func<AccountSummary, bool>> predicate = a => a.Owner == "Dan";

        _accountServiceMock
            .Setup(a => a.GetTotalMarketValueWherePredicateAsync(predicate, today))
            .ReturnsAsync(500);

        // Act
        var result = await _portfolioService.GetTotalMarketValueWhereExpressionAsync(predicate);

        // Assert
        Assert.Equal(500, result);
        _accountServiceMock.Verify(a => a.GetTotalMarketValueWherePredicateAsync(predicate, today), Times.Once);
    }

    [Fact]
    public async Task GroupBy_InMemory_GroupsAndSumsCorrectly()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var accounts = new List<AccountSummary>
            {
                new() {
                    Name="Dan-TD-TFSA-USD",
                    Owner = "Dan",
                    Type = AccountType.TFSA,
                    AccountFilter = AccountFilter.TFSA,
                    Bank = Bank.TD,
                    Cash = 0,
                    Currency = Currency.USD,
                    MarketValue = 100,
                    Date = today
                },
                new() {
                    Name="Dan-TD-RRSP-USD",
                    Owner = "Dan",
                    Type =AccountType.RRSP,
                    AccountFilter = AccountFilter.RRSP,
                    Bank = Bank.TD,
                    Cash = 0,
                    Currency = Currency.USD,
                    MarketValue = 200,
                    Date = today },
                new() {
                    Name = "Oana-WS-TFSA-USD",
                    Owner = "Oana",
                    Type = AccountType.TFSA,
                    AccountFilter = AccountFilter.TFSA,
                    Bank = Bank.WS,
                    Cash = 0,
                    Currency = Currency.USD,
                    MarketValue = 300,
                    Date = today }
            };

        _accountServiceMock
            .Setup(a => a.GetAccountSummariesAsync(today))
            .ReturnsAsync(accounts);

        // Act
        var result = await _portfolioService.GroupBy_InMemory(a => a.Owner);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(300, result["Dan"]);
        Assert.Equal(300, result["Oana"]);
    }

    [Fact]
    public async Task GetTotalMarketValueByOwnerAsync_DelegatesToWhereExpression()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        _accountServiceMock
            .Setup(a => a.GetTotalMarketValueWherePredicateAsync(It.IsAny<Expression<Func<AccountSummary, bool>>>(), today))
            .ReturnsAsync(400);

        // Act
        var result = await _portfolioService.GetTotalMarketValueByOwnerAsync("Dan");

        // Assert
        Assert.Equal(400, result);
    }

}
