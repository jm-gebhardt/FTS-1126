using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OrderGenerator = Gebhardt.Acc.PrismatCommunicationTest.Helper.OrderGenerator;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

public class DoppelspielTriggerPointEventHandlerTestFixture: MassTransitConsumerTestFixture<DoppelspielTriggerPointEventHandler>
{
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();
    private readonly Mock<IPrismatCommunicationHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<IPrismatCommunicationStationRepository> _stationRepository = new();
    private readonly Mock<IAgvSettingsService> _agvSettingsService = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{DoppelspielTriggerPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<DoppelspielTriggerPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<StationRepository>>().Object);
        collection.AddTransient(_ => new Mock<ILogger<DoppelspielTriggerPointEventHandler>>().Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        collection.AddSingleton<IStationRepository, StationRepository>();
        collection.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
        collection.AddSingleton(_ => _hostOrderRepository.Object);
        collection.AddTransient(_ => _agvSettingsService.Object);
        collection.AddTransient(_ => _transportOrderDb.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddSingleton<TargetStation>();
        return this;
    }

    /// <inheritdoc cref="DoppelspielTriggerPointEventHandler"/>>
    public DoppelspielTriggerPointEventHandlerTestFixture()
    {
        Build();
    }

    public DoppelspielTriggerPointEventHandlerTestFixture WithStationGroup(bool? isOccupied, bool hasHostOrders)
    {
        var station = new Station();
        if (isOccupied != null && isOccupied.Value)
        {
            station.AddAgv(Guid.NewGuid());
        }
        var stationGroup = isOccupied == null ? null : new StationGroup([station]);
        _stationRepository.Setup(x => x.GetStationGroup(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(stationGroup);
        _hostOrderRepository.Setup(x => x.HasCurrentHostOrdersForStationGroup(It.IsAny<ICollection<string>>(), It.IsAny<ICollection<string>>())).ReturnsAsync(hasHostOrders);
        return this;
    }

    public DoppelspielTriggerPointEventHandlerTestFixture WithHostOrders(HostOrder order, HostOrder? nextPickOrder = null)
    {
        if (nextPickOrder == null)
        {
            _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
            return this;
        }
        _hostOrderRepository.SetupSequence(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(order)
            .ReturnsAsync(nextPickOrder);
        return this;
    }

    public DoppelspielTriggerPointEventHandlerTestFixture WithAgvHasLowBatteryLevel(bool hasLowBatteryLevel)
    {
        _agvSettingsService.Setup(x => x.CheckAgvHasLowBatteryLevel(It.IsAny<Guid>())).ReturnsAsync(hasLowBatteryLevel);
        return this;
    }
}