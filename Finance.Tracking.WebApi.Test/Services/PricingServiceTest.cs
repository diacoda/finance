using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Finance.Tracking.Models;
using Finance.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Xunit;

namespace Finance.Tracking.Tests.Services;

public class PricingServiceTests
{
    private FinanceDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    private HttpClient CreateHttpClientMock(string html)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(html),
            })
            .Verifiable();

        return new HttpClient(handlerMock.Object, disposeHandler: true);
    }

    [Fact]
    public void GetPrice_ReturnsDefault_IfNotFound()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var yahooMock = new Mock<IYahooService>();
        var fakeHtml = @"<html><body><span id='last_last'>123.45</span></body></html>";
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => CreateHttpClientMock(fakeHtml)); // <-- returns new client each call

        var service = new PricingService(new[] { Symbol.TDB900 }, httpFactoryMock.Object, db, yahooMock.Object);

        // Act
        var result = service.GetPrice(Symbol.TDB900);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public async Task SavePriceAsync_Adds_NewEntry()
    {
        using var db = CreateInMemoryDbContext();
        var yahooMock = new Mock<IYahooService>();
        var httpFactory = Mock.Of<IHttpClientFactory>();

        var service = new PricingService(new[] { Symbol.TDB900 }, httpFactory, db, yahooMock.Object);

        // Act
        var rows = await service.SavePriceAsync(Symbol.TDB900, 123.45, DateOnly.FromDateTime(DateTime.Today));

        // Assert
        Assert.Equal(1, rows);
        var price = await db.Prices.FirstAsync();
        Assert.Equal(123.45, price.Value);
    }

    [Fact]
    public async Task SavePriceAsync_Updates_ExistingEntry()
    {
        using var db = CreateInMemoryDbContext();
        db.Prices.Add(new Price
        {
            Symbol = Symbol.TDB900,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Value = 10.0
        });
        await db.SaveChangesAsync();

        var yahooMock = new Mock<IYahooService>();
        var httpFactory = Mock.Of<IHttpClientFactory>();
        var service = new PricingService(new[] { Symbol.TDB900 }, httpFactory, db, yahooMock.Object);

        // Act
        var rows = await service.SavePriceAsync(Symbol.TDB900, 55.55, DateOnly.FromDateTime(DateTime.Today));

        // Assert
        Assert.Equal(1, rows);
        var price = await db.Prices.FirstAsync();
        Assert.Equal(55.55, price.Value);
    }

    [Fact]
    public async Task LoadPricesAsync_UsesYahooService_WhenNotCached()
    {
        using var db = CreateInMemoryDbContext();
        var yahooMock = new Mock<IYahooService>();
        yahooMock.Setup(y => y.GetPrice("TDB900.TO", It.IsAny<DateOnly?>()))
                 .ReturnsAsync(42.42);

        // Arrange
        var fakeHtml = @"<html><body><span id='last_last'>42.42</span></body></html>";

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => CreateHttpClientMock(fakeHtml)); // <-- returns new client each call

        //var httpFactory = Mock.Of<IHttpClientFactory>();
        var service = new PricingService(new[] { Symbol.TDB900 }, httpFactoryMock.Object, db, yahooMock.Object);

        // Act
        var prices = await service.LoadPricesAsync(DateOnly.FromDateTime(DateTime.Today));

        // Assert
        Assert.Equal(42.42, prices[Symbol.TDB900]);
    }

    [Fact]
    public async Task LoadPricesAsync_UsesTdScraper_WhenTdESeries()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var yahooMock = new Mock<IYahooService>();

        //var html = "<html><body><span id='last_last'>99.99</span></body></html>";
        //var httpClient = CreateHttpClientMock(html);

        // Arrange
        var fakeHtml = @"<html><body><span id='last_last'>99.99</span></body></html>";

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => CreateHttpClientMock(fakeHtml)); // <-- returns new client each call

        var service = new PricingService(new[] { Symbol.TDB902 }, httpFactoryMock.Object, db, yahooMock.Object);

        // Act
        var prices = await service.LoadPricesAsync(DateOnly.FromDateTime(DateTime.Today));

        // Assert
        Assert.Equal(99.99, prices[Symbol.TDB902]);
    }

    [Fact]
    public async Task GetPricesByDateAsync_ReturnsSortedPrices()
    {
        using var db = CreateInMemoryDbContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.Prices.AddRange(
            new Price { Symbol = Symbol.TDB902, Date = today, Value = 1 },
            new Price { Symbol = Symbol.TDB900, Date = today, Value = 2 }
        );
        await db.SaveChangesAsync();

        var yahooMock = new Mock<IYahooService>();
        var httpFactory = Mock.Of<IHttpClientFactory>();
        var service = new PricingService(new[] { Symbol.TDB900, Symbol.TDB902 }, httpFactory, db, yahooMock.Object);

        // Act
        var result = await service.GetPricesByDateAsync(today);

        // Assert → should be ordered by symbol enum
        Assert.Equal(2, result.Count);
        Assert.Equal(Symbol.TDB900, result[0].Symbol);
        Assert.Equal(Symbol.TDB902, result[1].Symbol);
    }
}
