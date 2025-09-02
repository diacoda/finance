using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Finance.Tracking.Controllers;
using Finance.Tracking.Tests;
using Finance.Tracking.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Finance.Tracking.Tests.Controllers;

public class HistoryControllerTests
{
    [Fact]
    public async Task GetHistoricalTotalMarketValue_ReturnsOkWithData()
    {
        // Arrange
        var mockService = new Mock<IHistoryService>();
        var sampleData = new List<TotalMarketValue>
            {
                new TotalMarketValue { AsOf = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), MarketValue = 1000.50 },
                new TotalMarketValue { AsOf = DateOnly.FromDateTime(DateTime.Today), MarketValue = 1010.75 }
            };

        mockService
            .Setup(s => s.GetHistoricalTotalMarkeValueAsync(It.IsAny<int?>()))
            .ReturnsAsync(sampleData);

        var controller = new HistoryController(mockService.Object);

        // Act
        var result = await controller.GetHistoricalTotalMarketValue(30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsAssignableFrom<List<TotalMarketValue>>(okResult.Value);

        Assert.Equal(2, returnedData.Count);
        Assert.Equal(sampleData[0].AsOf, returnedData[0].AsOf);
        Assert.Equal(sampleData[0].MarketValue, returnedData[0].MarketValue);
    }

    [Fact]
    public async Task GetHistoricalTotalMarketValue_CallsServiceWithCorrectParameter()
    {
        // Arrange
        var mockService = new Mock<IHistoryService>();
        mockService.Setup(s => s.GetHistoricalTotalMarkeValueAsync(It.IsAny<int?>()))
                   .ReturnsAsync(new List<TotalMarketValue>());

        var controller = new HistoryController(mockService.Object);
        int? days = 15;

        // Act
        await controller.GetHistoricalTotalMarketValue(days);

        // Assert
        mockService.Verify(s => s.GetHistoricalTotalMarkeValueAsync(days), Times.Once);
    }

    [Fact]
    public async Task GetHistoricalTotalMarketValue_ReturnsEmptyList_WhenNoData()
    {
        // Arrange
        var mockService = new Mock<IHistoryService>();
        mockService.Setup(s => s.GetHistoricalTotalMarkeValueAsync(It.IsAny<int?>()))
                   .ReturnsAsync(new List<TotalMarketValue>());

        var controller = new HistoryController(mockService.Object);

        // Act
        var result = await controller.GetHistoricalTotalMarketValue(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsAssignableFrom<List<TotalMarketValue>>(okResult.Value);

        Assert.Empty(returnedData);
    }
}
