using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using JetBrains.Annotations;
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
    [PublicAPI]
    public Mock<IStatisticsDb> StatisticsDbMock { get; set; } = new();

    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => StatisticsDbMock.Object);
        StatisticsDbMock.SetupVerifiableMethods();
        return this;
    }

    public MockDbConsumerTestFixture(TimeSpan? inactivityTimeout = null)
    {
        Build();
        if (inactivityTimeout != null)
        {
            TestHarness.TestInactivityTimeout = (TimeSpan)inactivityTimeout;
        }
    }

    public MockDbConsumerTestFixture<TConsumer> WithGrids(List<Grid> grids)
    {
        StatisticsDbMock.Setup(x => x.Grids.GetAll()).ReturnsAsync(grids);
        StatisticsDbMock.Setup(x => x.Grids.ThrowIfNotExistAsync(It.IsAny<Guid>()))
            .Callback((Guid gridId) =>
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
        StatisticsDbMock.Setup(x => x.Maps.GetMapsForGridAsync(It.IsAny<Guid>())).ReturnsAsync(maps);
        StatisticsDbMock.Setup(x => x.Maps.ThrowIfNotExistAsync(It.IsAny<Guid>())).Callback((Guid c) =>
        {
            if (maps.All(m => m.MapId != c))
            {
                throw new MapNotFoundException(c);
            }
        });
        return this;
    }

    public MockDbConsumerTestFixture<TConsumer> WithGridPositions(List<GridAgvPosition> positions)
    {
        StatisticsDbMock.Setup(x => x.GridAgvPositions.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => positions.Single(p => p.PositionId == id));
        StatisticsDbMock.Setup(x => x.GridAgvPositions.GetPositionAsync(It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<float>(),
                It.IsAny<string>()))
            .ReturnsAsync((Guid id,
                int x,
                int y,
                float z,
                string agv) => positions.Single(p =>
                p.GridId == id &&
                p.XIndex == x &&
                p.YIndex == y &&
                Math.Abs(p.Z - z) <= float.Epsilon &&
                p.SerialNumber == agv));
        return this;
    }

    public MockDbConsumerTestFixture<TConsumer> WithFieldViolationOccurence(CountHeatmap heatmap)
    {
        StatisticsDbMock.Setup(x => x.GridAgvFieldViolations.GetFieldViolationOccurence(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string[]>())).ReturnsAsync(heatmap);
        return this;
    }
}