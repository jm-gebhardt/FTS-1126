using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class AgvEventHandlerTestFixture : MassTransitConsumerTestFixture<AgvEventHandler>
{
    private readonly Mock<IAgvSagaCommunicationService> _agvSagaCommunicationService = new();
    private readonly Mock<IActionGroupRegistry> _actionGroupRegistry = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly Mock<IChargingService> _chargingService = new();
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();

    public const int BatteryLevelCritical = 10;
    public const int BatteryLevelLow = 40;
    public const int BatteryLevelGood = 80;
    public const int BatteryLevelFull = 100;

    private static readonly AgvSettings _agvSettings = new()
    {
        BatteryLevelCritical = BatteryLevelCritical,
        BatteryLevelFull = BatteryLevelFull,
        BatteryLevelGood = BatteryLevelGood,
        BatteryLevelLow = BatteryLevelLow,
        AgvOfflineTimeoutMsWhileIdle = 15000,
        AgvOfflineTimeoutMsWhileBusy = 20000
    };

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _agvSagaCommunicationService.Object);
        collection.AddTransient(_ => _actionGroupRegistry.Object);
        collection.AddTransient(_ => _maintenanceService.Object);
        collection.AddTransient(_ => _chargingService.Object);
        collection.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
        collection.AddAgvSettings(_agvSettings);
    }

    public AgvEventHandlerTestFixture()
    {
        Build();
    }

    public void AddAgv(IAgv agv)
    {
        _agvSagaCommunicationService.Setup(x =>
            x.GetAgvAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(agv);
    }

    public void SetAgvEligibleForCharging(bool isEligibleForCharging = false)
    {
        _chargingService.Setup(x => x.AgvIsEligibleForCharging(It.IsAny<IAgv>())).Returns(isEligibleForCharging);
    }

    public void SetupAgvStates(bool isReadyForBusy=true, bool needsCharging = false, bool isCarryingLoad = false, bool needsMaintenance= false)
    {
        _agvSagaCommunicationService.Setup(x =>
            x.IsAgvReadyForBusyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(isReadyForBusy);
        _chargingService.Setup(x => x.AgvNeedsCharging(It.IsAny<IAgv>())).Returns(needsCharging);
        _actionGroupRegistry.Setup(x => x.HasUnfinishedDropActionsAsync(It.IsAny<Guid>())).ReturnsAsync(isCarryingLoad);
        _maintenanceService.Setup(x => x.MaintenanceRequested(It.IsAny<IAgv>())).Returns(needsMaintenance);
    }
}