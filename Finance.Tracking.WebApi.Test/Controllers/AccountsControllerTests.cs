using Microsoft.AspNetCore.Mvc;
using Finance.Tracking.Controllers;

namespace Finance.Tracking.Tests.Controllers;

public class AccountsControllerTests
{
    private readonly Mock<IAccountService> _accountServiceMock = new();
    private readonly AccountsController _controller;

    public AccountsControllerTests()
    {
        _controller = new AccountsController(_accountServiceMock.Object);
    }

    [Fact]
    public async Task BuildSummaries_ReturnsOkMessage()
    {
        // Arrange
        DateOnly? date = DateOnly.FromDateTime(DateTime.Today);
        _accountServiceMock.Setup(s => s.BuildSummariesByDateAsync(date))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.BuildSummaries(date);
        var okResult = result as OkObjectResult;

        // Assert
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult!.StatusCode);
        Assert.Contains("Account summaries built", okResult.Value!.ToString());
    }

    [Fact]
    public async Task DeleteSummaries_ReturnsDeletedCount()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        _accountServiceMock.Setup(s => s.DeleteSummariesByDateAsync(date))
            .ReturnsAsync(3);

        // Act
        var result = await _controller.DeleteSummaries(date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value!;

        // Extract properties via reflection
        var deleted = (int)value.GetType().GetProperty("deleted")!.GetValue(value)!;
        Assert.Equal(3, deleted);
        string returnedDateString = (string)value.GetType().GetProperty("date")!.GetValue(value)!;
        DateOnly returnedDate = DateOnly.Parse(returnedDateString);
        Assert.Equal(date, returnedDate);
    }


    [Fact]
    public async Task GetLatestDates_ReturnsFormattedList()
    {
        // Arrange
        var dates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.Today) };
        _accountServiceMock.Setup(s => s.GetLast30AvailableDatesAsync())
            .ReturnsAsync(dates);

        // Act
        var result = await _controller.GetLatestDates();
        var okResult = result.Result as OkObjectResult;

        // Assert
        Assert.NotNull(okResult);
        var value = okResult!.Value as List<string>;
        Assert.NotNull(value);
        Assert.Single(value!);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"), value[0]);
    }
}
