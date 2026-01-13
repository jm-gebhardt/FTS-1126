using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Helper;
using Shouldly;
using AgvError = Gebhardt.Acc.Statistics.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfAgvErrorRepositoryTest
{
    private Grid _grid = null!;
    private Map _map = null!;
    private GridCell _gridCell = null!;
    private MapGridCell _mapGridCell = null!;

    [TestInitialize]
    public void Setup()
    {
        _grid = GridGenerator.GenerateGrid();
        _map = MapGenerator.GenerateMap(1, 1, 12, 12, null, _grid.GridId);
        _gridCell = new GridCell(1, 1, 1, 1, 2, 2, _grid);
        _mapGridCell = new MapGridCell(_map, _gridCell);
    }

    [TestMethod]
    public async Task GetErrorOccurence_ValidData_ReturnsScalarHeatmap()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_gridCell.CellId,
            agvError.AgvCorrelationId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid.GridId,
            _map.MapId,
            agvError.ErrorReceived.AddMinutes(-1),
            DateTime.UtcNow,
            [
                agvError.AgvCorrelationId
            ],
            agvError.ErrorLevel,
            agvError.ErrorCode);
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEquivalentTo(new List<ICollection<float>> { new List<float> { 1, 1, 1 } });
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoAgvsGiven_ReturnsScalarHeatmap()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_gridCell.CellId,
            agvError.AgvCorrelationId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid.GridId,
            _map.MapId,
            agvError.ErrorReceived.AddMinutes(-1),
            DateTime.UtcNow,
            [
            ],
            agvError.ErrorLevel,
            agvError.ErrorCode);
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEquivalentTo(new List<ICollection<float>> { new List<float> { 1, 1, 1 } });
    }

    [TestMethod]
    public async Task GetErrorOccurence_UnknownGrid_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_gridCell.CellId,
            agvError.AgvCorrelationId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence("unknown".ToGuid(),
            _map.MapId,
            agvError.ErrorReceived.AddMinutes(-1),
            DateTime.UtcNow,
            [
                agvError.AgvCorrelationId
            ],
            agvError.ErrorLevel,
            agvError.ErrorCode);
        heatmap.GridId.ShouldBe("unknown".ToGuid());
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_UnknownMap_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_gridCell.CellId,
            agvError.AgvCorrelationId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid.GridId,
            "unknown".ToGuid(),
            agvError.ErrorReceived.AddMinutes(-1),
            DateTime.UtcNow,
            [
                agvError.AgvCorrelationId
            ],
            agvError.ErrorLevel,
            agvError.ErrorCode);
        heatmap.MapId.ShouldBe("unknown".ToGuid());
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.Values.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoDataInTimeRange_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_gridCell.CellId,
            agvError.AgvCorrelationId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid.GridId,
            _map.MapId,
            agvError.ErrorReceived.AddMinutes(1),
            agvError.ErrorReceived.AddMinutes(2),
            [
                agvError.AgvCorrelationId
            ],
            agvError.ErrorLevel,
            agvError.ErrorCode);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.Values.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoErrorWithGivenLevel_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_gridCell.CellId,
            agvError.AgvCorrelationId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid.GridId,
            _map.MapId,
            agvError.ErrorReceived.AddMinutes(-1),
            DateTime.UtcNow,
            [
                agvError.AgvCorrelationId
            ],
            new List<ErrorLevel>
            {
                ErrorLevel.Fatal,
                ErrorLevel.Warning
            }.Single(e => e != error.ErrorLevel),
            agvError.ErrorCode);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.Values.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoErrorWithGivenErrorCode_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_gridCell.CellId,
            agvError.AgvCorrelationId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid.GridId,
            _map.MapId,
            agvError.ErrorReceived.AddMinutes(-1),
            DateTime.UtcNow,
            [
                agvError.AgvCorrelationId
            ],
            agvError.ErrorLevel,
            "unknown");
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoErrors_ReturnsScalarHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapGridCell.Add(_mapGridCell);
        fixture.GridCell.Add(_gridCell);
        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid.GridId,
            _map.MapId,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow,
            [
            ],
            ErrorLevel.Fatal,
            "a code");
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Values.ShouldBeEmpty();
    }
}