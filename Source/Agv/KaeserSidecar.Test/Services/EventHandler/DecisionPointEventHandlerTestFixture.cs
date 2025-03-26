using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using JetBrains.Annotations;
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
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();
    private readonly Mock<IPrismatCommunicationHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<IConveyorCommunicator> _communicator = new();
    private readonly Mock<IAgvSettingsService> _agvSettingsService = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{DecisionPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<DecisionPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<StationRepository>>().Object);
        collection.AddTransient(_ => new Mock<ILogger<DecisionPointEventHandler>>().Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        collection.AddSingleton<IStationRepository, StationRepository>();
        collection.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
        collection.AddSingleton(_ => _communicator.Object);
        collection.AddSingleton(_ => _hostOrderRepository.Object);
        collection.AddTransient(_ => _agvSettingsService.Object);
        collection.AddTransient(_ => _transportOrderDb.Object);
        collection.AddSingleton<TargetStation>();
        return this;
    }

    /// <inheritdoc cref="DecisionPointEventHandlerTestFixture"/>>
    public DecisionPointEventHandlerTestFixture()
    {
        _communicator.Setup(x => x.ReportPalletPickDropStation(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Verifiable();

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

    public DecisionPointEventHandlerTestFixture WithStations(ICollection<Station> stations)
    {
        _transportOrderDb.Setup(x => x.Stations.GetStationsByNames(It.IsAny<string[]>())).ReturnsAsync(stations);
        return this;
    }

    public DecisionPointEventHandlerTestFixture WithNextPickOrder(HostOrder? order)
    {
        _hostOrderRepository.Setup(x => x.GetNextIncompleteOrderWithUnknownPickTarget()).ReturnsAsync(order);
        return this;
    }

    public DecisionPointEventHandlerTestFixture WithCurrentHostOrder(HostOrder? order)
    {
        if (order != null)
        {
            _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
        }
        return this;
    }

    public DecisionPointEventHandlerTestFixture WithAgvHasLowBatteryLevel(bool hasLowBatteryLevel)
    {
        _agvSettingsService.Setup(x => x.CheckAgvHasLowBatteryLevel(It.IsAny<Guid>())).ReturnsAsync(hasLowBatteryLevel);
        return this;
    }

    [PublicAPI]
    public void VerifyReportingToConveyors(string station="none", string? dropStation = null, string? pickStation=null, Times? times=null)
    {
        times ??= Times.Once();
        if (times == Times.Never())
        {
            _communicator.Verify(x => x.ReportPalletPickDropStation(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), (Times)times);
        }
        _communicator.Verify(x => x.ReportPalletPickDropStation(station, pickStation, dropStation), (Times)times);
    }
}