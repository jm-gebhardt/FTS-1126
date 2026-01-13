using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

[TestClass]
public class AgvPositionReceivedHandlerTest
{
    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_SinglePositionAdded()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId",
            new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.VerifyCellIdsRequested();
        await fixture.ShouldNotHavePublishedAsync<AddGridCellStay>();
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_MultipleGrids_MultipleGridsAdded()
    {
        var grids = new List<Grid> { GridGenerator.GenerateGrid(), GridGenerator.GenerateGrid() };
        var map = MapGenerator.GenerateMap();
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.VerifyCellIdsRequested();
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_WhenNotWithinMapXYIndex_AgvMapPositionAdded()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 41, 59, 0, 0)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        fixture.VerifyCellIdsRequested();
        fixture.VerifyAgvMapPositionsAdded(0);
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_PublishesAddGridCellStay()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        const string agvSerialNumber = "agv";
        var agvCorrelationId = agvSerialNumber.ToGuid();
        var gridCell = new GridCell(5, 6, 5, 6, 6, 7, grid);
        MockDbConsumerTestFixture<AgvPositionReceivedHandler> fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithLastEntryTime(agvCorrelationId, gridCell, DateTime.UtcNow)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.PublishMessageAsync(new AgvPositionReceived(agvCorrelationId, agvSerialNumber,
            new AgvPosition(true, 6, 5, 0, 0)));

        var leftGridCell = await fixture.GetFirstPublishedMessageAsync<AddGridCellStay>();
        leftGridCell.GridCellStay.GridCellId.ShouldBe(gridCell.CellId);
    }
}