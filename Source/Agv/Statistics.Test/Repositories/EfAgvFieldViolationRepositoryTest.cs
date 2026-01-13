using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfAgvFieldViolationRepositoryTest
{
    private Grid _grid = null!;
    private Map _map = null!;
    private GridCell _gridCell = null!;
    private MapGridCell _mapGridCell = null!;
    private Guid _agvCorrelationId;

    [TestInitialize]
    public void Setup()
    {
        _grid = GridGenerator.GenerateGrid();
        _map = MapGenerator.GenerateMap(1, 1, 2, 2, null, _grid.GridId);
        _gridCell = new GridCell(1, 1, 1m, 2m, 1m, 2m, _grid);
        _mapGridCell = new MapGridCell(_map, _gridCell);
        _agvCorrelationId = "agv".ToGuid();
    }

    [TestMethod]
    public async Task GetFieldViolationOccurence_ValidData_ReturnsScalarHeatmap()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvFieldViolation = new AgvFieldViolation(_agvCorrelationId, _gridCell.CellId, DateTime.UtcNow);
        fixture.GridAgvFieldViolations.Add(agvFieldViolation);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvFieldViolations.GetFieldViolationOccurence(_grid.GridId,
            _map.MapId,
            agvFieldViolation.Timestamp.AddMinutes(-1),
            DateTime.UtcNow,
            [
                agvFieldViolation.AgvCorrelationId
            ]);

        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEquivalentTo(new List<ICollection<float>> { new List<float> { 1, 1, 1 } });
    }

    [TestMethod]
    public async Task GetFieldViolationOccurence_UnknownGrid_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvFieldViolation = new AgvFieldViolation(_agvCorrelationId, _gridCell.CellId, DateTime.UtcNow);
        fixture.GridAgvFieldViolations.Add(agvFieldViolation);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvFieldViolations.GetFieldViolationOccurence("unknown".ToGuid(),
            _map.MapId,
            agvFieldViolation.Timestamp.AddMinutes(-1),
            DateTime.UtcNow,
            [
                agvFieldViolation.AgvCorrelationId
            ]);

        heatmap.GridId.ShouldBe("unknown".ToGuid());
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetFieldViolationOccurence_UnknownMap_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvFieldViolation = new AgvFieldViolation(_agvCorrelationId, _gridCell.CellId, DateTime.UtcNow);
        fixture.GridAgvFieldViolations.Add(agvFieldViolation);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvFieldViolations.GetFieldViolationOccurence(_grid.GridId,
            "unknown".ToGuid(),
            agvFieldViolation.Timestamp.AddMinutes(-1),
            DateTime.UtcNow,
            [
                agvFieldViolation.AgvCorrelationId
            ]);

        heatmap.MapId.ShouldBe("unknown".ToGuid());
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.Values.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetFieldViolationOccurence_NoDataInTimeRange_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvFieldViolation = new AgvFieldViolation(_agvCorrelationId, _gridCell.CellId, DateTime.UtcNow);
        fixture.GridAgvFieldViolations.Add(agvFieldViolation);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvFieldViolations.GetFieldViolationOccurence(_grid.GridId,
            _map.MapId,
            agvFieldViolation.Timestamp.AddMinutes(1),
            agvFieldViolation.Timestamp.AddMinutes(2),
            [
                agvFieldViolation.AgvCorrelationId
            ]);

        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetFieldViolationOccurence_NoFieldViolations_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvFieldViolations.GetFieldViolationOccurence(_grid.GridId,
            _map.MapId,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow,
            [
            ]);

        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEmpty();
    }
}