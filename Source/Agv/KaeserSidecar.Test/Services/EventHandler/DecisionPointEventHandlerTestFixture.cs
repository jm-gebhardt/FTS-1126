using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

/// <summary>
/// Provides a test fixture for <see cref="DecisionPointEventHandler"/>
/// </summary>
public class DecisionPointEventHandlerTestFixture: MassTransitConsumerTestFixture<DecisionPointEventHandler>
{
    private readonly IStationRepository _repository;

    /// <inheritdoc cref="MassTransitConsumerTestFixture{DecisionPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<DecisionPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<StationRepository>>().Object);
        collection.AddTransient(_ => new Mock<ILogger<DecisionPointEventHandler>>().Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        collection.AddSingleton<IStationRepository, StationRepository>();
        return this;
    }

    /// <inheritdoc cref="DecisionPointEventHandlerTestFixture"/>>
    public DecisionPointEventHandlerTestFixture()
    {
        Build();
        _repository = Provider.GetRequiredService<IStationRepository>();
    }

    /// <summary>
    /// Add occupancies to repository
    /// </summary>
    public DecisionPointEventHandlerTestFixture WithOccupancies(ICollection<StationOccupancy> occupancies)
    {
        foreach (var occupancy in occupancies)
        {
            foreach (int unused in Enumerable.Range(0,occupancy.Count))
            {
                _repository.OccupyStation(occupancy.Station, Guid.NewGuid());
            }
        }
        return this;
    }
}