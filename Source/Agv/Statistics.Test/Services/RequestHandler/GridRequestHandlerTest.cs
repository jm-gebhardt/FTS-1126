using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Messages.Requests;
using Gebhardt.Acc.Statistics.Services.RequestResponseHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Helper;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.RequestHandler;

[TestClass]
public class GridRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetGrids_ValidData_PublishesGotGrids()
    {
        var grid = GridGenerator.GenerateGrid();
        var fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithGrids([grid]);

        await fixture.PublishMessageAsync(new GetGrids(Guid.NewGuid()));

        fixture.StatisticsDbMock.Verify(x => x.Grids.GetGridsAsync(), Times.Once);

        var gotGrids = await fixture.GetFirstPublishedMessageAsync<GotGrids>();
        gotGrids.Grids.Count.ShouldBe(1);
        gotGrids.Grids[0].ShouldBe(grid);
    }

    [TestMethod]
    public async Task Consume_GetStayDurations_ValidData_PublishesGotStayDurations()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var agvCorrelationIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, [[0, 0, 100], [1, 0, 150]]);

        var fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithStayDurations(heatMap)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.PublishMessageAsync(new GetStayDurations
        {
            GridId = grid.GridId,
            MapId = map.MapId,
            FromDate = fromDate,
            ToDate = toDate,
            AgvCorrelationIds = agvCorrelationIds
        });

        fixture.StatisticsDbMock.Verify(x => x.GridCellStay.GetStayDurations(
            grid.GridId,
            map.MapId,
            fromDate,
            toDate,
            agvCorrelationIds), Times.Once);

        var gotStayDurations = await fixture.GetFirstPublishedMessageAsync<GotStayDurations>();
        gotStayDurations.PositionFrequency.ShouldBe(heatMap);
    }

    [TestMethod]
    public async Task Consume_GetErrorFrequencies_ValidData_PublishesGotStayDurations()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var agvCorrelationIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        const ErrorLevel errorLevel = ErrorLevel.Warning;
        const string errorCode = "Error";
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, [[0, 0, 5], [1, 0, 3]]);

        var fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithErrorOccurence(heatMap)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.PublishMessageAsync(new GetErrorFrequencies
        {
            GridId = grid.GridId,
            MapId = map.MapId,
            FromDate = fromDate,
            ToDate = toDate,
            AgvCorrelationIds = agvCorrelationIds,
            ErrorLevel = errorLevel,
            ErrorCode = errorCode
        });

        fixture.StatisticsDbMock.Verify(x => x.GridAgvErrors.GetErrorOccurence(
            grid.GridId,
            map.MapId,
            fromDate,
            toDate,
            agvCorrelationIds,
            errorLevel,
            errorCode), Times.Once);

        var gotStayDurations = await fixture.GetFirstPublishedMessageAsync<GotStayDurations>();
        gotStayDurations.PositionFrequency.ShouldBe(heatMap);
    }

    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_NoFieldViolationsUnknownGrid_DoesNotPublishGotStayDurations()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, []);
        var fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies
        {
            GridId = "Unknown".ToGuid(),
            MapId = map.MapId,
            AgvCorrelationIds = []
        });

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldNotHavePublishedAsync<GotStayDurations>();
    }

    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_NoFieldViolationsUnknownMap_DoesNotPublishGotStayDurations()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, []);
        var fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies
        {
            GridId = grid.GridId,
            MapId = "Unknown".ToGuid(),
            AgvCorrelationIds = []
        });

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldNotHavePublishedAsync<GotStayDurations>();
    }

    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_ValidData_PublishesGotStayDurations()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var agvCorrelationIds = new[] { Guid.NewGuid() };
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, [[0, 0, 2], [1, 0, 4]]);

        var fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.PublishMessageAsync(new GetFieldViolationFrequencies
        {
            GridId = grid.GridId,
            MapId = map.MapId,
            FromDate = fromDate,
            ToDate = toDate,
            AgvCorrelationIds = agvCorrelationIds
        });

        fixture.StatisticsDbMock.Verify(x => x.GridAgvFieldViolations.GetFieldViolationOccurence(
            grid.GridId,
            map.MapId,
            fromDate,
            toDate,
            agvCorrelationIds), Times.Once);

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldHavePublishedAsync<GotStayDurations>();
    }

    [TestMethod]
    public async Task Consume_GetWifiSignalStrength_ValidData_PublishesGotWifiSignalStrength()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var agvCorrelationIds = new[] { Guid.NewGuid() };
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, [[0, 0, -70], [1, 0, -65], [0, 1, -75]]);

        var fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithWifiSignalStrength(heatMap)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.PublishMessageAsync(new GetWifiSignalStrength
        {
            GridId = grid.GridId,
            MapId = map.MapId,
            FromDate = fromDate,
            ToDate = toDate,
            AgvCorrelationIds = agvCorrelationIds
        });

        fixture.StatisticsDbMock.Verify(x => x.GridAgvWifiMetrics.GetWifiSignalStrengthHeatmapAsync(
            grid.GridId,
            map.MapId,
            fromDate,
            toDate,
            agvCorrelationIds), Times.Once);

        await fixture.ShouldHaveConsumedAsync<GetWifiSignalStrength>();
        var wifiStrength = await fixture.GetFirstPublishedMessageAsync<GotWifiSignalStrength>();
        wifiStrength.SignalStrength.ShouldBe(heatMap);
    }
}