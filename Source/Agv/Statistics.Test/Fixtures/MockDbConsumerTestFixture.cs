using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Statistics.Domain.Models;
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
        return this;
    }
    
    public MockDbConsumerTestFixture<TConsumer> WithMaps(List<Map> maps)
    {
        StatisticsDbMock.Setup(x => x.Maps.GetMapsForGridAsync(It.IsAny<Guid>())).ReturnsAsync(maps);
        return this;
    }
}