using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvError = Gebhardt.Acc.Statistics.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfAgvErrorRepositoryTest
{
    private  Grid? _grid ;
    private  Map? _map ;
    private GridAgvPosition? _position;
    private MapAgvPositions? _mapPosition;

    [TestInitialize]
    public void Setup()
    {
        _grid  = new Grid("1x1", 12, 12, 1, 1, 1, 1);
        _map = new Map(Guid.NewGuid(), _grid.GridId, 1, 1, 2, 2, 1);
        _position = new GridAgvPosition(_grid.GridId, "agv", 1, 1, 1, 1, 1, DateTime.UtcNow, _grid);
        _mapPosition = new MapAgvPositions(_map.MapId, _map, _position.PositionId, _position);
    }

    [TestMethod]
    public async Task GetErrorOccurence_ValidData_ReturnsCountHeatmap()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_position!.PositionId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid!.GridId, _map!.MapId, _position.Timestamp.AddMinutes(-1), DateTime.UtcNow, [_position.SerialNumber], agvError.ErrorLevel, agvError.ErrorCode);
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Frequencies.ShouldBeEquivalentTo(new List<ICollection<float>> { new List<float> { 1, 1, 1 } });
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoAgvsGiven_ReturnsCountHeatmap()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_position!.PositionId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid!.GridId, _map!.MapId, _position.Timestamp.AddMinutes(-1), DateTime.UtcNow, [], agvError.ErrorLevel, agvError.ErrorCode);
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Frequencies.ShouldBeEquivalentTo(new List<ICollection<float>> { new List<float> { 1, 1, 1 } });
    }

    [TestMethod]
    public async Task GetErrorOccurence_UnknownGrid_ReturnsCountHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_position!.PositionId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence("unknown".ToGuid(), _map!.MapId, _position.Timestamp.AddMinutes(-1), DateTime.UtcNow, [_position.SerialNumber], agvError.ErrorLevel, agvError.ErrorCode);
        heatmap.GridId.ShouldBe("unknown".ToGuid());
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Frequencies.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_UnknownMap_ReturnsCountHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_position!.PositionId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid!.GridId, "unknown".ToGuid(), _position.Timestamp.AddMinutes(-1), DateTime.UtcNow, [_position.SerialNumber], agvError.ErrorLevel, agvError.ErrorCode);
        heatmap.MapId.ShouldBe("unknown".ToGuid());
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.Frequencies.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoDataInTimeRange_ReturnsCountHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_position!.PositionId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid!.GridId, _map!.MapId, _position.Timestamp.AddMinutes(1), _position.Timestamp.AddMinutes(2), [_position.SerialNumber], agvError.ErrorLevel, agvError.ErrorCode);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.Frequencies.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoErrorWithGivenLevel_ReturnsCountHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_position!.PositionId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid!.GridId, _map!.MapId, _position.Timestamp.AddMinutes(-1), DateTime.UtcNow, [_position.SerialNumber], new List<ErrorLevel> { ErrorLevel.Fatal , ErrorLevel.Warning}.Single(e => e != error.ErrorLevel), agvError.ErrorCode);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.Frequencies.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoErrorWithGivenErrorCode_ReturnsCountHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_position!.PositionId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid!.GridId, _map!.MapId, _position.Timestamp.AddMinutes(-1), DateTime.UtcNow, [_position.SerialNumber], agvError.ErrorLevel, "unknown");
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Frequencies.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetErrorOccurence_NoErrors_ReturnsCountHeatmapWithEmptyFrequencies()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        await fixture.SaveAsync();

        var heatmap = await fixture.GridAgvErrors.GetErrorOccurence(_grid!.GridId, _map!.MapId, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, [_position!.SerialNumber], ErrorLevel.Fatal, "a code");
        heatmap.GridId.ShouldBe(_grid.GridId);
        heatmap.MapId.ShouldBe(_map.MapId);
        heatmap.Frequencies.ShouldBeEmpty();
    }
}