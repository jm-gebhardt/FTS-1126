using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OrderGenerator = Gebhardt.Acc.PrismatCommunicationTest.Helper.OrderGenerator;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

public class DoppelspielTriggerPointEventHandlerTestFixture : MassTransitConsumerTestFixture<DoppelspielTriggerPointEventHandler>
{
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();
    private readonly Mock<IPrismatCommunicationHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<IAgvSettingsService> _agvSettingsService = new();
    private readonly Mock<IAklAgvLimitGuard> _aklAgvLimitGuard = new();

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
        collection.AddTransient(_ => _aklAgvLimitGuard.Object);
        return this;
    }

    /// <inheritdoc cref="DoppelspielTriggerPointEventHandler"/>>
    public DoppelspielTriggerPointEventHandlerTestFixture()
    {
        Build();
    }

    public DoppelspielTriggerPointEventHandlerTestFixture WithinAklAgvLimit(bool isWithinLimit)
    {
        _aklAgvLimitGuard.Setup(x => x.IsWithinAklAgvLimitAsync(It.IsAny<HostOrder>())).ReturnsAsync(isWithinLimit);
        return this;
    }

    public DoppelspielTriggerPointEventHandlerTestFixture WithHostOrder(HostOrder order)
    {
        _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
        return this;
    }

    public DoppelspielTriggerPointEventHandlerTestFixture WithNextPickOrder(HostOrder? order)
    {
        _hostOrderRepository.Setup(x => x.GetNextPickForSources(It.IsAny<ICollection<string>>())).ReturnsAsync(order);
        return this;
    }

    public DoppelspielTriggerPointEventHandlerTestFixture WithAgvHasCriticalBatteryLevel(bool hasLowBatteryLevel)
    {
        _agvSettingsService.Setup(x => x.CheckAgvHasCriticalBatteryLevel(It.IsAny<Guid>())).ReturnsAsync(hasLowBatteryLevel);
        return this;
    }
}