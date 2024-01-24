using Gebhardt.StoreWare.Agv.Common.Testing;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Statistics.Domain.Models;
using Statistics.Domain.Repositories;

namespace Statistics.Test.Fixtures;

/// <summary>
/// Provides a consumer test fixture with a mock db.
/// </summary>
public class MockDbConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
{
    [PublicAPI]
    public Mock<IStatisticsDb> StatisticsDbMock { get; set; } = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => StatisticsDbMock.Object);
        StatisticsDbMock.SetupVerifiableMethods();
    }

    public MockDbConsumerTestFixture(TimeSpan? inactivityTimeout = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        params Action<IServiceCollection>[] services) : base(busRegistrationConfigurator, inMemoryConfiguration, services)
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
}