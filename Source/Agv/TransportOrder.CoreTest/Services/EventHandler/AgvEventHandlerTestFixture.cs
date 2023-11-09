using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class AgvEventHandlerTestFixture : MassTransitConsumerTestFixtureWithScheduler<AgvEventHandler>
{
    private readonly Mock<IActionGroupRegistry> _actionGroupRegistry = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public const int BatteryLevelCritical = 10;
    public const int BatteryLevelLow = 40;
    public const int BatteryLevelGood = 80;
    public const int BatteryLevelFull = 100;

    public static readonly AgvSettings AgvSettings = new()
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
        collection.AddTransient(_ => _actionGroupRegistry.Object);
        collection.AddTransient(_ => _maintenanceService.Object);
        collection.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _agvRepository.Object);
        collection.AddAgvSettings(AgvSettings);
    }

    public AgvEventHandlerTestFixture()
    {
        Build();
    }

    public void AddAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvRepository.AddAgvsToMockRepository(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
    }

    public void AgvHasUnfinishedDropAction(Guid correlationId)
    {
        _actionGroupRegistry.Setup(r => r.HasUnfinishedDropActionsAsync(It.Is<Guid>(c => c == correlationId))).ReturnsAsync(true);
        _actionGroupRegistry.Setup(r => r.HasUnfinishedActionGroupAsync(It.Is<Guid>(c => c == correlationId))).ReturnsAsync(true);
    }

    public static IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(new AgvStatusProfile());
        return mapper;
    }
}
