using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Services.EventHandler;
using Gebhardt.StoreWare.Agv.Statistics.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.Statistics.Test.EventHandler;

[TestClass]
public class PositionEventHandlerTest
{
    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_SinglePositionAdded()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        MockDbConsumerTestFixture<PositionEventHandler> fixture = new MockDbConsumerTestFixture<PositionEventHandler>().WithGrids(grids);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_MultipleGrids_MultipleGridPositionsAdded()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2), new("a grid", 10, 10, 0, 0, 1, 1) };
        MockDbConsumerTestFixture<PositionEventHandler> fixture = new MockDbConsumerTestFixture<PositionEventHandler>().WithGrids(grids);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
    }
}