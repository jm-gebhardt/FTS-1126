using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Messages.Requests;
using Gebhardt.Acc.Statistics.Services.RequestResponseHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.RequestHandler;

[TestClass]
public class GridRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_NoFieldViolationsUnknownGrid_DoesNotPublishGotStayDurations()
    {
        var grid = new Grid("a grid", 2, 2);
        var map = new Map(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95);
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
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
        var grid = new Grid("a grid", 2, 2);
        var map = new Map(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95);
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
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
        var grid = new Grid("a grid", 2, 2);
        var map = new Map(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95);
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies
        {
            GridId = grid.GridId,
            MapId = map.MapId,
            AgvCorrelationIds = []
        });

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldHavePublishedAsync<GotStayDurations>();
    }

    [TestMethod]
    public async Task Consume_GetWifiSignalStrength_ValidData_PublishesGotStayDurations()
    {
        var grid = new Grid("a grid", 2, 2);
        var map = new Map(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95);
        var heatMap = new ScalarHeatmap(grid.GridId, map.MapId, [[0, 0, -70], [1, 0, -65], [0, 1, -75]]);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithWifiSignalStrength(heatMap)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new GetWifiSignalStrength
        {
            GridId = grid.GridId,
            MapId = map.MapId,
            AgvCorrelationIds = []
        });

        await fixture.ShouldHaveConsumedAsync<GetWifiSignalStrength>();
        var wifiStrength = await fixture.GetFirstPublishedMessageAsync<GotWifiSignalStrength>();
        wifiStrength.SignalStrength.ShouldBe(heatMap);
    }
}