using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

public class AgvEventHandlerTestFixture : MassTransitConsumerTestFixture<AgvEventHandler>
{
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly SemaphoreProvider<IAgvsSemaphore> _agvSemaphoreProvider = new();
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    public const int BatteryLevelCritical = 10;
    public const int BatteryLevelLow = 40;
    public const int MinChargeUp = 80;
    public const int MaxChargeUp = 100;

    private static readonly AgvSettingsReadModel _agvSettingsReadModel = new(15000, 20000, BatteryLevelCritical,BatteryLevelLow, MinChargeUp, MaxChargeUp);

    protected override IEnumerable<Profile> GetProfiles() => [new AgvStatusProfile()];

    public static readonly TransportOrder.Messages.MessageContent.AgvSettings AgvSettingsMessage = new()
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
        collection.AddTransient(_ => _nodeStateRepository.Object);
        return this;
    }

    public AgvEventHandlerTestFixture()
    {
        Build();
    }

    public AgvEventHandlerTestFixture WithAgvGraph(AgvGraph agvGraph)
    {
        _agvDb.AddAgvGraph(agvGraph);
        return this;
    }

    public void AddAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
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

    public AgvEventHandlerTestFixture WithActionGroups(List<ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<ActionGroupState>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 0, It.IsAny<Guid>(), It.IsAny<bool?>())).ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(
            It.IsAny<IList<ActionGroupState>?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<Guid?>(),
            It.IsAny<bool?>(),
            It.IsAny<SortBy>(),
            It.IsAny<bool>()
        )).ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetCreatedAndStartedActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetPendingActionGroupsForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroups);

        return this;
    }
}