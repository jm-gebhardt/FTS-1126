using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfAgvWifiMetricsRepositoryTest
{
    private Grid _grid = null!;
    private Map _map = null!;
    private GridCell _gridCell = null!;
    private MapGridCell _mapGridCell = null!;

    [TestInitialize]
    public void Setup()
    {
        _grid = GridGenerator.GenerateGrid();
        _map = MapGenerator.GenerateMap(1, 1, 2, 2, null, _grid.GridId);
        _gridCell = GridCellGenerator.GenerateGridCell(grid: _grid);
        _mapGridCell = new MapGridCell(_map, _gridCell);
    }

    [TestMethod]
    public async Task GetWifiMetricsHeatmapAsync_SingleSample_ReturnsSameValue()
    {
        await using var fixture = new StatisticsDbTestFixture();

        fixture.Grids.Add(_grid);
        fixture.Maps.Add(_map);
        fixture.GridCell.Add(_gridCell);
        fixture.MapGridCell.Add(_mapGridCell);

        fixture.GridAgvWifiMetrics.Add(AgvWifiMetricsGenerator.GenerateWifiMetrics(_gridCell, beaconSignalAverage: -42));
        await fixture.SaveAsync();

        var result = await fixture.GridAgvWifiMetrics.GetWifiSignalStrengthHeatmapAsync(
            _grid.GridId,
            _map.MapId,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(1),
            []
        );

        result.Values.ShouldHaveSingleItem();
        var row = result.Values.Single().ToList();
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
        fixture.GridCell.Add(_gridCell);
        fixture.MapGridCell.Add(_mapGridCell);

        fixture.GridAgvWifiMetrics.Add(AgvWifiMetricsGenerator.GenerateWifiMetrics(_gridCell, beaconSignalAverage: -60));
        fixture.GridAgvWifiMetrics.Add(AgvWifiMetricsGenerator.GenerateWifiMetrics(_gridCell, beaconSignalAverage: -70));
        fixture.GridAgvWifiMetrics.Add(AgvWifiMetricsGenerator.GenerateWifiMetrics(_gridCell, beaconSignalAverage: -80));
        await fixture.SaveAsync();

        var result = await fixture.GridAgvWifiMetrics.GetWifiSignalStrengthHeatmapAsync(
            _grid.GridId,
            _map.MapId,
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow,
            []
        );

        result.GridId.ShouldBe(_grid.GridId);
        result.MapId.ShouldBe(_map.MapId);

        // -60 dBm -> 10^-6 mW
        // -70 dBm -> 10^-7 mW
        // -80 dBm -> 10^-8 mW
        // avg_mW = (10^-6 + 10^-7 + 10^-8) / 3 = 3.7*10^-7
        // avg_dBm = 10 * log10(3.7*10-7) ≈ -64.3

        result.Values.ShouldHaveSingleItem();
        var row = result.Values.Single().ToList();
        row[0].ShouldBe(1);
        row[1].ShouldBe(1);
        row[2].ShouldBe(-64.3f, 0.1f);
    }
}