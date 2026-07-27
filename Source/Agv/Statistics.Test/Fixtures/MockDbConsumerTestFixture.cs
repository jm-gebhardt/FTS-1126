using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

/// <summary>
/// Provides a consumer test fixture with a mock db.
/// </summary>
public class MockDbConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
{
    private readonly Mock<IGridCellRepository> _gridCellRepoMock = new();

    private readonly Mock<IMapGridCellRepository> _mapGridCellRepoMock = new();

    public Mock<IStatisticsDb> StatisticsDbMock { get; } = new();

    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => StatisticsDbMock.Object);
        StatisticsDbMock.SetupGet(x => x.GridCell).Returns(_gridCellRepoMock.Object);
        StatisticsDbMock.SetupGet(x => x.MapGridCell).Returns(_mapGridCellRepoMock.Object);

        StatisticsDbMock.SetupVerifiableMethods();
        return this;
    }

    public MockDbConsumerTestFixture(TimeSpan? inactivityTimeout = null)
    {
        Build();
        if (inactivityTimeout != null)
        {
            TestHarness.TestInactivityTimeout = (TimeSpan) inactivityTimeout;
        }
    }

    public MockDbConsumerTestFixture<TConsumer> WithGrids(List<Grid> grids)
    {
        StatisticsDbMock.Setup(x => x.Grids.GetGridsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(grids);
        _gridCellRepoMock.Setup(x => x.GetOrAddGridCellsForCoordinatesAsync(It.IsAny<decimal>(), It.IsAny<decimal>())).ReturnsAsync([]);
        StatisticsDbMock.Setup(x => x.Grids.ThrowIfNotExistAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback((Guid gridId, CancellationToken _) =>
            {
                if (grids.All(a => a.GridId != gridId))
                {
                    throw new GridNotFoundException(gridId);
                }
            });

        return this;
    }

    public MockDbConsumerTestFixture<TConsumer> WithMaps(List<Map> maps)
    {
        StatisticsDbMock.Setup(x => x.Maps.GetMapAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => maps.Single(m => m.MapId == id));
        StatisticsDbMock.Setup(x => x.Maps.ThrowIfNotExistAsync(It.IsAny<Guid>())).Callback((Guid c) =>
        {
            if (maps.All(m => m.MapId != c))
            {
                throw new MapNotFoundException(c);
            }
        });
        return this;
    }

    public MockDbConsumerTestFixture<TConsumer> WithGridCells(List<GridCell> gridCells)
    {
        StatisticsDbMock.Setup(x => x.GridCell.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => gridCells.Single(p => p.CellId == id));
        StatisticsDbMock.Setup(x => x.GridCell.GetOrAddGridCellsForCoordinatesAsync(It.IsAny<decimal>(), It.IsAny<decimal>()))
            .ReturnsAsync((decimal x, decimal y) => gridCells.Where(p =>
                    p.XMin <= x && p.XMax >= x && p.YMin <= y && p.YMax >= y)
                .ToList());
        return this;
    }

    public MockDbConsumerTestFixture<TConsumer> WithStayDurations(ScalarHeatmap heatMap)
    {
        StatisticsDbMock.Setup(x => x.GridCellStay.GetStayDurations(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid[]>()))
            .ReturnsAsync(heatMap);
        return this;
    }

    public MockDbConsumerTestFixture<TConsumer> WithErrorOccurence(ScalarHeatmap heatMap)
    {
        StatisticsDbMock.Setup(x => x.GridAgvErrors.GetErrorOccurence(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<Guid[]>(),
            It.IsAny<ErrorLevel>(),
            It.IsAny<string?>())).ReturnsAsync(heatMap);
        return this;
    }

    public MockDbConsumerTestFixture<TConsumer> WithFieldViolationOccurence(ScalarHeatmap heatmap)
    {
        StatisticsDbMock.Setup(x => x.GridAgvFieldViolations.GetFieldViolationOccurence(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<Guid[]>())).ReturnsAsync(heatmap);
        return this;
    }

    public MockDbConsumerTestFixture<TConsumer> WithWifiSignalStrength(ScalarHeatmap heatmap)
    {
        StatisticsDbMock.Setup(x => x.GridAgvWifiMetrics.GetWifiSignalStrengthHeatmapAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid[]>()))
            .ReturnsAsync(heatmap);
        return this;
    }

    public void VerifyCellIdsRequested()
    {
        _gridCellRepoMock.Verify(x => x.GetOrAddGridCellsForCoordinatesAsync(It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Exactly(1));
    }

    public void VerifyAgvMapPositionsAdded(int count)
    {
        _mapGridCellRepoMock.Verify(x => x.Add(It.IsAny<MapGridCell>()), Times.Exactly(count));
        if (count > 0)
        {
            StatisticsDbMock.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}