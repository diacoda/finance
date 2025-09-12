using System.Net;
using System.Net.Http.Json;

namespace Finance.Tracking.Tests.Integration;

public class HistoryControllerIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HistoryControllerIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHistoricalTotalMarketValue_ReturnsOkWithData()
    {
        // Arrange
        var sampleData = new List<TotalMarketValue>
        {
            new TotalMarketValue { AsOf = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), MarketValue = 1000.50, Type= TotalMarketValueType.Total },
            new TotalMarketValue { AsOf = DateOnly.FromDateTime(DateTime.Today), MarketValue = 1010.75 , Type = TotalMarketValueType.Total}
        };
        _factory.HistoryServiceMock
            .Setup(s => s.GetHistoricalTotalMarkeValueAsync(It.IsAny<int?>()))
            .ReturnsAsync(sampleData);

        // Act
        var response = await _client.GetAsync("/api/history?days=30");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter(),
                new DateOnlyJsonConverter() // if you have DateOnly properties
            }
        };

        var returnedData = await response.Content.ReadFromJsonAsync<List<TotalMarketValue>>(options);

        //var returnedData = await response.Content.ReadFromJsonAsync<List<TotalMarketValue>>();
        Assert.NotNull(returnedData);
        Assert.Equal(2, returnedData.Count);
        Assert.Equal(sampleData[0].AsOf, returnedData[0].AsOf);
        Assert.Equal(sampleData[0].MarketValue, returnedData[0].MarketValue);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(100)]
    public async Task GetHistoricalTotalMarketValue_InvalidDays_ReturnsBadRequest(int days)
    {
        // Arrange: service throws ArgumentException for invalid days
        _factory.HistoryServiceMock
            .Setup(s => s.GetHistoricalTotalMarkeValueAsync(It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Days must be a positive integer or cannot exceed 30"));

        // Act
        var response = await _client.GetAsync($"/api/history?days={days}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
