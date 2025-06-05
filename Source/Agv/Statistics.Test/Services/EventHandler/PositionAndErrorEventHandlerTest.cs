using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using MassTransit.Internals;
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

[TestClass]
public class PositionAndErrorEventHandlerTest
{
    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_SinglePositionAdded()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        var maps = new List<Map> { new(Guid.NewGuid(),  Guid.NewGuid(), 65,27,70,95, 1)};
        MockDbConsumerTestFixture<PositionAndErrorEventHandler> fixture = new PositionAndErrorEventHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
    }
    
    [TestMethod]
    public async Task ConsumeAgvPositionReceived_ShouldNotAct_RepositoryUntouched()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2) };
        MockDbConsumerTestFixture<PositionAndErrorEventHandler> fixture = new PositionAndErrorEventHandlerTestFixture()
            .ShouldActReturns(false)
            .WithGrids(grids);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(0);
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_MultipleGrids_MultipleGridPositionsAdded()
    {
        var grids = new List<Grid> { new("a grid", 20, 20, 0, 0, 2, 2), new("a grid", 10, 10, 0, 0, 1, 1) };
        var maps = new List<Map> { new(Guid.NewGuid(),  Guid.NewGuid(), 65,27,70,95, 1)};
        MockDbConsumerTestFixture<PositionAndErrorEventHandler> fixture = new PositionAndErrorEventHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
    }
    
    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_WhenWithinMapXYIndex_AgvMapPositionAdded()
    {
        var grids = new List<Grid> { new("a grid", 1, 1, 0, 1, 0, 1) };
        var maps = new List<Map> { new(Guid.NewGuid(),  Guid.NewGuid(), 15,25,48,70, 1)};
        MockDbConsumerTestFixture<PositionAndErrorEventHandler> fixture = new PositionAndErrorEventHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 41,59, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
        fixture.StatisticsDbMock.VerifyAgvMapPositionsAdded(grids.Count);
    }
    
    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_WhenNotWithinMapXYIndex_AgvMapPositionAdded()
    {
        var grids = new List<Grid> { new("a grid", 1, 1, 0, 1, 0, 1) };
        var maps = new List<Map> { new(Guid.NewGuid(),  Guid.NewGuid(), 65,27,70,95, 1)};
        MockDbConsumerTestFixture<PositionAndErrorEventHandler> fixture = new PositionAndErrorEventHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps(maps);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 41,59, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(grids.Count);
        fixture.StatisticsDbMock.VerifyAgvMapPositionsAdded(0);
    }
    
    [TestMethod]
    public async Task ConsumeAgvErrorReceived_ValidData_PublishesAddError()
    {
        var grid = new Grid("1x1", 12, 12, 1, 1, 1, 1);
        var maps = new List<Map> { new(Guid.NewGuid(),  Guid.NewGuid(), 65,27,70,95, 1)};
        var position = new GridAgvPosition(grid.GridId, "agv", 1, 1, 1, 1, 1, DateTime.UtcNow, grid);
        var agvPosition = new AgvPosition(true, position.X, position.Y, 1,0);
        MockDbConsumerTestFixture<PositionAndErrorEventHandler> fixture = new PositionAndErrorEventHandlerTestFixture()
            .WithGrids([grid])
            .WithGridPositions([position])
            .WithMaps(maps);
        var error = AgvErrorGenerator.GenerateAgvError();

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(position.SerialNumber.ToGuid(), position.SerialNumber, agvPosition));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();
        await fixture.TestHarness.Bus.Publish(new AgvErrorReceived(error, agvPosition, position.SerialNumber));
        (await fixture.ConsumerHarness.Consumed.Any<AgvErrorReceived>()).ShouldBeTrue();
        
        (await fixture.TestHarness.Published.Any<AddError>()).ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task ErrorCache_PositionArrivesLater_ProcessesStoredErrors()
    {
        var grid = new Grid("1x1", 12, 12, 1, 1, 1, 1);
        var maps = new List<Map> { new(Guid.NewGuid(),  Guid.NewGuid(), 65,27,70,95, 1)};
        MockDbConsumerTestFixture<PositionAndErrorEventHandler> fixture = new PositionAndErrorEventHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids([grid])
            .WithMaps(maps);
        
        const string agvId = "testAgv";
        var position = new AgvPosition(true, 5, 5, 0, 0);
        var error = AgvErrorGenerator.GenerateAgvError();
    
        await fixture.TestHarness.Bus.Publish(new AgvErrorReceived(error, position, agvId));
    
        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(agvId.ToGuid(), agvId, position));
    
        (await fixture.TestHarness.Published.Any<AddError>()).ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task ErrorCache_TwoNonSignificantPositions_StoresErrorAndPosition()
    {
        var grid = new Grid("1x1", 12, 12, 1, 1, 1, 1);
        var maps = new List<Map> { new(Guid.NewGuid(),  Guid.NewGuid(), 65,27,70,95, 1)};
        var fixture = (new PositionAndErrorEventHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids([grid])
            .WithMaps(maps) as PositionAndErrorEventHandlerTestFixture)!;
        
        const string agvId = "testAgv";
        var position1 = new AgvPosition(true, 5, 5, 0, 0);
        var position2 = new AgvPosition(true, 5.001f, 5.001f, 0, 0);
        var error = AgvErrorGenerator.GenerateAgvError();
    
        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(agvId.ToGuid(), agvId, position1));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();
        fixture.ShouldActReturns(false);
        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(agvId.ToGuid(), agvId, position2));
        (await fixture.ConsumerHarness.Consumed.SelectAsync<AgvPositionReceived>().Take(2).ToListAsync()).Count.ShouldBe(2);
        
        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(1);
        
        await fixture.TestHarness.Bus.Publish(new AgvErrorReceived(error, position2, agvId));
        (await fixture.ConsumerHarness.Consumed.Any<AgvErrorReceived>()).ShouldBeTrue();
        fixture.StatisticsDbMock.VerifyAgvPositionsAdded(2, false);
        fixture.StatisticsDbMock.Verify(x => x.GridAgvPositions.Add(It.Is<GridAgvPosition>(g=>Math.Abs(g.X - position2.X) < 0.0001)), Times.Once);
    
        (await fixture.TestHarness.Published.Any<AddError>()).ShouldBeTrue();
    }
}