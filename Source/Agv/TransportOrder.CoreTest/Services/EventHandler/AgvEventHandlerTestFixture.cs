using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
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

public class AgvEventHandlerTestFixture : MassTransitConsumerTestFixture<AgvEventHandler>
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

    private static readonly AgvSettingsReadModel _agvSettingsReadModel = new(15000, 20000, BatteryLevelCritical,BatteryLevelLow, MinChargeUp, MaxChargeUp);

    protected override IEnumerable<Profile> GetProfiles() => [new AgvStatusProfile()];

    public static readonly Messages.MessageContent.AgvSettings AgvSettingsMessage = new()
    {
        BatteryLevelCritical = _agvSettingsReadModel.BatteryLevelCritical,
        MaxChargeUp = _agvSettingsReadModel.MaxChargeUp,
        MinChargeUp = _agvSettingsReadModel.MinChargeUp,
        BatteryLevelLow = _agvSettingsReadModel.BatteryLevelLow,
        AgvOfflineTimeoutMsWhileIdle = _agvSettingsReadModel.AgvOfflineTimeoutMsWhileIdle,
        AgvOfflineTimeoutMsWhileBusy = _agvSettingsReadModel.AgvOfflineTimeoutMsWhileBusy
    };

    protected override MassTransitConsumerTestFixture<AgvEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _maintenanceService.Object);
        collection.AddSingleton<ISemaphoreProvider<IAgvsSemaphore>>(_ => _agvSemaphoreProvider);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddTransient(_ => _agvDb.Object);
        return this;
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
}