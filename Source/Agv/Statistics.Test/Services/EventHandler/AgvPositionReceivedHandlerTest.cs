using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Messages.Commands;
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
        var fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId",
            new AgvPosition(true, 1, 1, 0, 0)));
        await fixture.ShouldHaveConsumedAsync<AgvPositionReceived>();

        fixture.VerifyCellIdsRequested();
        await fixture.ShouldNotHavePublishedAsync<AddGridCellStay>();
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_MultipleGrids_MultipleGridsAdded()
    {
        var grids = new List<Grid> { GridGenerator.GenerateGrid(), GridGenerator.GenerateGrid() };
        var map = MapGenerator.GenerateMap();
        var fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids(grids)
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 1, 1, 0, 0)));
        await fixture.ShouldHaveConsumedAsync<AgvPositionReceived>();

        fixture.VerifyCellIdsRequested();
    }

    [TestMethod]
    public async Task ConsumeAgvPositionReceived_SingleGrid_WhenNotWithinMapXYIndex_AgvMapPositionAdded()
    {
        var grid = GridGenerator.GenerateGrid();
        var map = MapGenerator.GenerateMap();
        var fixture = new AgvPositionReceivedHandlerTestFixture()
            .ShouldActReturns(true)
            .WithGrids([grid])
            .WithMaps([map]);

        await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(Guid.NewGuid(), "agvId", new AgvPosition(true, 41, 59, 0, 0)));
        await fixture.ShouldHaveConsumedAsync<AgvPositionReceived>();

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
        var gridCell = GridCellGenerator.GenerateGridCell(xIndex: 5, yIndex: 6, xMin: 5, xMax: 6, yMin: 6, yMax: 7, grid: grid);
        var fixture = new AgvPositionReceivedHandlerTestFixture()
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