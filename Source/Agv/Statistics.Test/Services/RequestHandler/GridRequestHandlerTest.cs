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
    public async Task Consume_GetFieldViolationFrequencies_NoFieldViolationsUnknownGrid_DoesNotPublishGotPositionFrequencies()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        var heatMap = new ScalarHeatmap(grids.Single().GridId, maps.Single().MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies
        {
            GridId = "Unknown".ToGuid(),
            MapId = maps.Single().MapId,
            AgvSerialNumbers = []
        });

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldNotHavePublishedAsync<GotPositionFrequencies>();
    }

    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_NoFieldViolationsUnknownMap_DoesNotPublishGotPositionFrequencies()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        var heatMap = new ScalarHeatmap(grids.Single().GridId, maps.Single().MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies
        {
            GridId = grids.Single().GridId,
            MapId = "Unknown".ToGuid(),
            AgvSerialNumbers = []
        });

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldNotHavePublishedAsync<GotPositionFrequencies>();
    }

    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_ValidData_PublishesGotPositionFrequencies()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        var heatMap = new ScalarHeatmap(grids.Single().GridId, maps.Single().MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies
        {
            GridId = grids.Single().GridId,
            MapId = maps.Single().MapId,
            AgvSerialNumbers = []
        });

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldHavePublishedAsync<GotPositionFrequencies>();
    }

    [TestMethod]
    public async Task Consume_GetWifiSignalStrength_ValidData_PublishesGotPositionFrequencies()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        var heatMap = new ScalarHeatmap(grids.Single().GridId, maps.Single().MapId, [[0, 0, -70], [1, 0, -65], [0, 1, -75]]);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithWifiSignalStrength(heatMap)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new GetWifiSignalStrength
        {
            GridId = grids.Single().GridId,
            MapId = maps.Single().MapId,
            AgvSerialNumbers = []
        });

        await fixture.ShouldHaveConsumedAsync<GetWifiSignalStrength>();
        var wifiStrength = await fixture.GetFirstPublishedMessageAsync<GotWifiSignalStrength>();
        wifiStrength.SignalStrength.ShouldBe(heatMap);
    }
}