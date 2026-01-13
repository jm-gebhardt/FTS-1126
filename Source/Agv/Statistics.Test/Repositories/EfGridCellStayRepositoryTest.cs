using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfGridCellStayRepositoryTest
{
    private Grid _grid = null!;
    private Map _map = null!;
    private GridCell _gridCell = null!;
    private MapGridCell _mapGridCell = null!;
    private GridCellStay _cellStay = null!;

    [TestInitialize]
    public void Setup()
    {
        _grid = GridGenerator.GenerateGrid();
        _map = MapGenerator.GenerateMap(1, 1, 2, 2, null, _grid.GridId);
        _gridCell = new GridCell(1, 1, 1, 2, 1, 2, _grid);
        _mapGridCell = new MapGridCell(_map, _gridCell);
        _cellStay = new GridCellStay("agv".ToGuid(), _gridCell, DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task DeleteAgvPositionsOlderThan_DeletesItemIfOlder()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        fixture.GridCellStay.Add(_cellStay);
        await fixture.SaveAsync();

        await fixture.GridCellStay.DeleteAgvPositionsOlderThan(_cellStay.Timestamp);
        (await fixture.GridCellStay.GetAll()).Count.ShouldBe(1);
        await fixture.GridCellStay.DeleteAgvPositionsOlderThan(_cellStay.Timestamp.AddMilliseconds(1));
        (await fixture.GridCellStay.GetAll()).Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetPositionAsync_ValidData_CellStayReturned()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        fixture.GridCellStay.Add(_cellStay);
        await fixture.SaveAsync();

        var stayDurations = await fixture.GridCellStay.GetStayDurations(_gridCell.GridId,
            _map.MapId,
            _cellStay.Timestamp.AddSeconds(-5),
            _cellStay.Timestamp.AddSeconds(5),
            []);
        stayDurations.Values.Count.ShouldBe(1);
        stayDurations.Values.First().ShouldBe(new List<float> { _gridCell.XIndex, _gridCell.YIndex, 1 });
    }
}