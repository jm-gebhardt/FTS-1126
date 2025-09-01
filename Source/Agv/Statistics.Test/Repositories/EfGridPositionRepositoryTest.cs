using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfGridPositionRepositoryTest
{
    private Grid? _grid;
    private Map? _map;
    private GridAgvPosition? _position;
    private MapAgvPositions? _mapPosition;

    [TestInitialize]
    public void Setup()
    {
        _grid = new Grid("1x1", 12, 12, 1, 1, 1, 1);
        _map = new Map(Guid.NewGuid(), _grid.GridId, 1, 1, 2, 2, 1);
        _position = new GridAgvPosition(_grid.GridId, "agv", 1, 1, 1, 1, 1, DateTime.UtcNow, _grid);
        _mapPosition = new MapAgvPositions(_map.MapId, _map, _position.PositionId, _position);
    }

    [TestMethod]
    public async Task DeleteAgvPositionOlderThan_PositionReferencedByError_NotDeleted()
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

        await fixture.GridAgvPositions.DeleteAgvPositionsOlderThan(DateTime.UtcNow);

        (await fixture.GridAgvPositions.GetByIdAsync(_position.PositionId)).ShouldBe(_position);
    }

    [TestMethod]
    public async Task DeleteAgvPositionOlderThan_PositionReferencedByFieldViolation_NotDeleted()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var violation = new AgvFieldViolation(_position!.PositionId, DateTime.UtcNow);
        fixture.GridAgvFieldViolations.Add(violation);
        await fixture.SaveAsync();

        await fixture.GridAgvPositions.DeleteAgvPositionsOlderThan(DateTime.UtcNow);

        (await fixture.GridAgvPositions.GetByIdAsync(_position.PositionId)).ShouldBe(_position);
    }

    [TestMethod]
    public async Task DeleteAgvPositionOlderThan_PositionReferencedByFieldViolationAndError_NotDeleted()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        var violation = new AgvFieldViolation(_position!.PositionId, DateTime.UtcNow);
        fixture.GridAgvFieldViolations.Add(violation);
        var agvError = AgvErrorGenerator.GenerateAgvError();
        var error = new AgvError(_position!.PositionId,
            agvError.ErrorReceived,
            agvError.ErrorLevel,
            agvError.ErrorCode ?? "",
            agvError.ErrorType,
            agvError.ErrorCategory);
        fixture.GridAgvErrors.Add(error);
        await fixture.SaveAsync();

        await fixture.GridAgvPositions.DeleteAgvPositionsOlderThan(DateTime.UtcNow);

        (await fixture.GridAgvPositions.GetByIdAsync(_position.PositionId)).ShouldBe(_position);
    }

    [TestMethod]
    public async Task DeleteAgvPositionOlderThan_NotReferencedPosition_Deleted()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        await fixture.SaveAsync();

        await fixture.GridAgvPositions.DeleteAgvPositionsOlderThan(DateTime.UtcNow);

        (await fixture.GridAgvPositions.GetAll()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetPositionAsync_ValidData_PositionReturned()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        await fixture.SaveAsync();

        (await fixture.GridAgvPositions.GetPositionAsync(_position!.GridId, _position!.XIndex, _position.YIndex, _position.Z, _position.SerialNumber)).ShouldBe(_position);
    }

    [TestMethod]
    public async Task GetPositionAsync_ZPositionWithinZIdentityPrecision_PositionReturned()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        await fixture.SaveAsync();

        (await fixture.GridAgvPositions.GetPositionAsync(_position!.GridId, _position!.XIndex, _position.YIndex, _position.Z + Domain.Constants.Grid.ZIdentityPrecision, _position.SerialNumber)).ShouldBe(_position);
    }

    [TestMethod]
    public async Task GetPositionAsync_NoPosition_ThrowsGridAgvPositionNotFoundException()
    {
        await using var fixture = new StatisticsDbTestFixture();
        await fixture.SaveAsync();

        await Should.ThrowAsync<GridAgvPositionNotFoundException>(fixture.GridAgvPositions.GetPositionAsync(_position!.GridId, _position!.XIndex, _position.YIndex, _position.Z, _position.SerialNumber));
    }

    [TestMethod]
    public async Task GetPositionAsync_ZPositionOutsideZIdentityPrecision_ThrowsGridAgvPositionNotFoundException()
    {
        await using var fixture = new StatisticsDbTestFixture();
        fixture.MapAgvPositions.Add(_mapPosition!);
        fixture.GridAgvPositions.Add(_position!);
        fixture.Grids.Add(_grid!);
        fixture.Maps.Add(_map!);
        await fixture.SaveAsync();

        await Should.ThrowAsync<GridAgvPositionNotFoundException>(fixture.GridAgvPositions.GetPositionAsync(_position!.GridId, _position!.XIndex, _position.YIndex, _position.Z + 1.01f*Domain.Constants.Grid.ZIdentityPrecision, _position.SerialNumber));
    }
}