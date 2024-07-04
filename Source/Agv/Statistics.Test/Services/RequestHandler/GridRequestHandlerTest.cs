using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Messages.Requests;
using Gebhardt.Acc.Statistics.Services.RequestResponseHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.Statistics.Test.Services.RequestHandler;

[TestClass]
public class GridRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_NoFieldViolationsUnknownGrid_DoesNotPublishGotPositionFrequencies()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        var heatMap = new CountHeatmap(grids.Single().GridId, maps.Single().MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies("Unknown".ToGuid(), maps.Single().MapId, null, null, []));

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldNotHavePublishedAsync<GotPositionFrequencies>();
    }

    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_NoFieldViolationsUnknownMap_DoesNotPublishGotPositionFrequencies()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        var heatMap = new CountHeatmap(grids.Single().GridId, maps.Single().MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies(grids.Single().GridId, "Unknown".ToGuid(), null, null, []));

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldNotHavePublishedAsync<GotPositionFrequencies>();
    }

    [TestMethod]
    public async Task Consume_GetFieldViolationFrequencies_ValidData_PublishesGotPositionFrequencies()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        var heatMap = new CountHeatmap(grids.Single().GridId, maps.Single().MapId, []);
        MockDbConsumerTestFixture<GridRequestHandler> fixture = new MockDbConsumerTestFixture<GridRequestHandler>()
            .WithFieldViolationOccurence(heatMap)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new GetFieldViolationFrequencies(grids.Single().GridId, maps.Single().MapId, null, null, []));

        await fixture.ShouldHaveConsumedAsync<GetFieldViolationFrequencies>();
        await fixture.ShouldHavePublishedAsync<GotPositionFrequencies>();
    }
}