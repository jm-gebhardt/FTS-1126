using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Events;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

[TestClass]
public class AgvPositionReceivedHandlerTest
{
    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_SinglePositionAdded()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
        await fixture.ShouldHavePublishedAsync<GridPositionPersisted>();
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_ShouldNotAct_RepositoryUntouched()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(false)
            .WithGrids(grids);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(0);
        await fixture.ShouldNotHavePublishedAsync<GridPositionPersisted>();
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_MultipleGrids_MultipleGridPositionsAdded()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2), new("a grid", 10, 10, 0, 0, 1, 1) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
        await fixture.ShouldHavePublishedAsync<GridPositionPersisted>();
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_WhenWithinMapXYIndex_AgvMapPositionAdded()
    {
        var grids = new List<Grid> { new("a grid", 1, 1, 0, 1, 0, 1) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 15, 25, 48, 70, 1) };
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 41, 59, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
        fixture.StatisticsDbMock.VerifyAgvMapPositionsAdded(grids.Count);
        await fixture.ShouldHavePublishedAsync<GridPositionPersisted>();
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_WhenNotWithinMapXYIndex_AgvMapPositionAdded()
    {
        var grids = new List<Grid> { new("a grid", 1, 1, 0, 1, 0, 1) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 41, 59, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
        fixture.StatisticsDbMock.VerifyAgvMapPositionsAdded(0);
        await fixture.ShouldHavePublishedAsync<GridPositionPersisted>();
    }

    [TestMethod]
    public async Task ConsumeForcePersistAgvPosition_SingleGrid_SinglePositionAdded()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(), Guid.NewGuid(), 65, 27, 70, 95, 1) };
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new ForcePersistAgvPosition("agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<ForcePersistAgvPosition>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
        await fixture.ShouldHavePublishedAsync<GridPositionPersisted>();
    }
}