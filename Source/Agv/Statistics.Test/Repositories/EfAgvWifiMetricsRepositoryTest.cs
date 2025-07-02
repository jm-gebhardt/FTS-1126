using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfAgvWifiMetricsRepositoryTest
{
    private Grid _grid = null!;
    private Map _map = null!;
    private GridAgvPosition _position = null!;
    private MapAgvPositions _mapPosition = null!;

    [TestInitialize]
    public void Setup()
    {
        _grid = new Grid("1x1", 12, 12, 1, 1, 1, 1);
        _map = new Map(Guid.NewGuid(), _grid.GridId, 1, 1, 2, 2, 1);
        _position = new GridAgvPosition(_grid.GridId, "agv", 1, 1, 1, 1, 1, DateTime.UtcNow, _grid);
        _mapPosition = new MapAgvPositions(_map.MapId, _map, _position.PositionId, _position);
    }

    [TestMethod]
    public async Task GetWifiMetricsHeatmapAsync_SingleSample_ReturnsSameValue()
    {
        await using var fixture = new StatisticsDbTestFixture();

        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        fixture.GridAgvPositions.Add(_position);
        fixture.MapAgvPositions.Add(_mapPosition);

        fixture.GridAgvWifiMetrics.Add(AgvWifiMetricsGenerator.GenerateWifiMetrics(_position.PositionId, beaconSignalAverage: -42));
        await fixture.SaveAsync();

        var result = await fixture.GridAgvWifiMetrics.GetWifiSignalStrengthHeatmapAsync(
            _grid.GridId,
            _map.MapId,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(1),
            [_position.SerialNumber]
        );

        result.Values.ShouldHaveSingleItem();
        List<float> row = result.Values.Single().ToList();
        row[0].ShouldBe(1);
        row[1].ShouldBe(1);
        row[2].ShouldBe(-42f, 0.001f);
    }

    [TestMethod]
    public async Task GetWifiMetricsHeatmapAsync_AveragesCorrectlyAcrossSamples()
    {
        await using var fixture = new StatisticsDbTestFixture();

        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        fixture.GridAgvPositions.Add(_position);
        fixture.MapAgvPositions.Add(_mapPosition);

        fixture.GridAgvWifiMetrics.Add(AgvWifiMetricsGenerator.GenerateWifiMetrics(_position.PositionId, beaconSignalAverage: -60));
        fixture.GridAgvWifiMetrics.Add(AgvWifiMetricsGenerator.GenerateWifiMetrics(_position.PositionId, beaconSignalAverage: -70));
        fixture.GridAgvWifiMetrics.Add(AgvWifiMetricsGenerator.GenerateWifiMetrics(_position.PositionId, beaconSignalAverage: -80));
        await fixture.SaveAsync();

        var result = await fixture.GridAgvWifiMetrics.GetWifiSignalStrengthHeatmapAsync(
            _grid.GridId,
            _map.MapId,
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow,
            [_position.SerialNumber]
        );

        result.GridId.ShouldBe(_grid.GridId);
        result.MapId.ShouldBe(_map.MapId);

        // -60 dBm -> 10^-6 mW
        // -70 dBm -> 10^-7 mW
        // -80 dBm -> 10^-8 mW
        // avg_mW = (10^-6 + 10^-7 + 10^-8) / 3 = 3.7*10^-7
        // avg_dBm = 10 * log10(3.7*10-7) ≈ -64.3

        result.Values.ShouldHaveSingleItem();
        List<float> row = result.Values.Single().ToList();
        row[0].ShouldBe(1);
        row[1].ShouldBe(1);
        row[2].ShouldBe(-64.3f, 0.1f);
    }
}