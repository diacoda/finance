using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Finance.Tracking.Models.Yahoo;
using Finance.Tracking.Services;
using Moq;
using Moq.Protected;
using Xunit;
using System.Text.Json;

namespace Finance.Tracking.Tests.Services;

public class YahooServiceTests
{
    [Fact]
    public async Task GetPrice_ReturnsPrice_WhenResponseIsValid()
    {
        // Arrange
        var symbol = "VFV.TO";
        var targetDate = DateOnly.FromDateTime(DateTime.Today);

        // Prepare fake Yahoo JSON response
        var fakeResponse = new Response
        {
            chart = new Chart
            {
                result = new[]
                {
                        new Result
                        {
                            timestamp = new long[] { DateTimeOffset.Now.ToUnixTimeSeconds() },
                            indicators = new Indicators
                            {
                                quote = new[]
                                {
                                    new Quote { close = new double[] { 123.45 } }
                                }
                            }
                        }
                    }
            }
        };

        var json = JsonSerializer.Serialize(fakeResponse);

        // Mock HttpClient
        var handlerMock = new Mock<HttpMessageHandler>();
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
                Content = new StringContent(json)
            });

        var client = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var service = new YahooService(factoryMock.Object);

        // Act
        var price = await service.GetPrice(symbol, targetDate);

        // Assert
        Assert.Equal(123.45, price);
    }

    [Fact]
    public async Task GetPrice_ThrowsException_WhenResponseIsInvalid()
    {
        // Arrange
        var symbol = "VFV.TO";
        var invalidJson = "{}"; // missing required fields

        var handlerMock = new Mock<HttpMessageHandler>();
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
                Content = new StringContent(invalidJson)
            });

        var client = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var service = new YahooService(factoryMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetPrice(symbol));
    }

    [Fact]
    public void PickRange_ReturnsCorrectRange()
    {
        // Arrange
        var factoryMock = new Mock<IHttpClientFactory>();
        var service = new YahooService(factoryMock.Object);

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Act & Assert
        Assert.Equal("1d", service.GetType().GetMethod("PickRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(service, new object[] { today })!);

        Assert.Equal("5d", service.GetType().GetMethod("PickRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(service, new object[] { today.AddDays(-3) })!);

        Assert.Equal("1mo", service.GetType().GetMethod("PickRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(service, new object[] { today.AddDays(-20) })!);

        Assert.Equal("3mo", service.GetType().GetMethod("PickRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(service, new object[] { today.AddDays(-60) })!);

        Assert.Equal("6mo", service.GetType().GetMethod("PickRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(service, new object[] { today.AddDays(-120) })!);

        Assert.Equal("1y", service.GetType().GetMethod("PickRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(service, new object[] { today.AddDays(-300) })!);

        Assert.Equal("max", service.GetType().GetMethod("PickRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(service, new object[] { today.AddDays(-400) })!);
    }
}
