using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Configurations;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class AgvEventHandlerTestFixture : MassTransitConsumerTestFixtureWithScheduler<AgvEventHandler>
{
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly SemaphoreProvider<IAgvsSemaphore> _agvSemaphoreProvider = new();
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    public const int BatteryLevelCritical = 10;
    public const int BatteryLevelLow = 40;
    public const int MinChargeUp = 80;
    public const int MaxChargeUp = 100;

    public static readonly AgvSettings AgvSettings = new()
    {
        BatteryLevelCritical = BatteryLevelCritical,
        MaxChargeUp = MaxChargeUp,
        MinChargeUp = MinChargeUp,
        BatteryLevelLow = BatteryLevelLow,
        AgvOfflineTimeoutMsWhileIdle = 15000,
        AgvOfflineTimeoutMsWhileBusy = 20000
    };

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _maintenanceService.Object);
        collection.AddTransient<ISemaphoreProvider<IAgvsSemaphore>>(_ => _agvSemaphoreProvider);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddFakeSettings(AgvSettings);
        collection.AddTransient(_ => _agvDb.Object);
    }

    public AgvEventHandlerTestFixture()
    {
        Build();
    }

    public AgvEventHandlerTestFixture WithAgvGraph(AgvGraph agvGraph)
    {
        _agvDb.Setup(x => x.Graphs.GetAgvGraphAsync(It.IsAny<Guid>())).ReturnsAsync(agvGraph);
        return this;
    }

    public void AddAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvsToMockRepository(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
    }

    public void AddChargeAction(ActionGroup actionGroup = null!, bool existsPriorToTest = true)
    {
        if (existsPriorToTest)
        {
            _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(new List<ActionGroup> { actionGroup });
        }
        else
        {
            _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync(new List<ActionGroup>());
        }
    }

    public void AgvHasUnfinishedDropAction(Guid correlationId)
    {
        _actionGroupRepository.Setup(r => r.HasUnfinishedDropActionsAsync(It.Is<Guid>(c => c == correlationId))).ReturnsAsync(true);
        _actionGroupRepository.Setup(r => r.HasUnfinishedActionGroupAsync(It.Is<Guid>(c => c == correlationId))).ReturnsAsync(true);
    }

    public static IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(new AgvStatusProfile());
        return mapper;
    }
}