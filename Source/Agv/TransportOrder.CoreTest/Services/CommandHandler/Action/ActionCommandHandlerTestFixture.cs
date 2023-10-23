using AutoMapper;
using FluentValidation.Results;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

public class ActionCommandHandlerTestFixture : MassTransitConsumerTestFixture<ActionCommandHandler>
{
    private readonly Mock<IRouteService> _routeService = new();
    private readonly Mock<IActionGroupRegistry> _actionGroupRegistry = new();
    private readonly Mock<IChooseAgvActionGroupService> _chooseAgvActionGroupService = new();
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();
    private readonly Mock<IMaintenanceService> _maintenanceService = new();
    private readonly Mock<IChargingPointManager> _chargingPointManager = new();
    private readonly Mock<IParkPointService> _parkPointService = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();
    private readonly Mock<IActionGroupValidationService> _actionGroupValidationService = new();
    private readonly Mock<ISettingsProvider<AgvSettings>> _settingsProvider = new();
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalService = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _actionGroupRegistry.Object);
        collection.AddTransient(_ => _routeService.Object);
        collection.AddTransient(_ => _chooseAgvActionGroupService.Object);
        collection.AddTransient(_ => _maintenanceService.Object);
        collection.AddTransient(_ => _chargingPointManager.Object);
        collection.AddTransient(_ => _parkPointService.Object);
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _agvRepository.Object);
        collection.AddTransient(_ => _actionGroupValidationService.Object);
        collection.AddTransient(_ => _settingsProvider.Object);
        collection.AddTransient(_ => _actionGroupRetrievalService.Object);
    }

    public ActionCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null)
    {
        _settingsProvider.Setup(s => s.GetSettings()).Returns(new AgvSettings()
        {
            BatteryLevelCritical = 10,
            BatteryLevelGood = 80,
            BatteryLevelFull = 100,
            BatteryLevelLow = 20,
            AgvOfflineTimeoutMsWhileBusy = 10000,
            AgvOfflineTimeoutMsWhileIdle = 10000
        });
        Build();
        if (inactivityTimeout != null)
        {
            TestHarness!.TestInactivityTimeout = (TimeSpan)inactivityTimeout;
        }
        _actionGroupRegistry.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?)null);
        _actionGroupRegistry.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).Verifiable();
    }

    public void AddAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvRepository.AddAgvsToMockRepository(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        _agvRepository.AddAgvsToMockRepository(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
    }

    public void AddActionGroup(ActionGroup actionGroup, bool hasUnfinishedAction = true,  bool hasNotStartedAction = true)
    {
        _actionGroupRegistry.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRegistry.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(), It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<ActionState>())).ReturnsAsync(actionGroup);
        _actionGroupRegistry.Setup(x => x.GetSubsequentActionAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());
        _actionGroupRegistry.Setup(x => x.GetActionGroupsAsync(It.IsAny<ActionGroupState>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<bool?>())).ReturnsAsync(new List<ActionGroup> { actionGroup });
        _actionGroupRegistry.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRegistry.Setup(x => x.HasNotStartedActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasNotStartedAction);
        _actionGroupRegistry.Setup(x => x.HasUnfinishedDropActionsAsync(It.IsAny<Guid>())).ReturnsAsync(hasUnfinishedAction);
        _actionGroupRegistry.Setup(x => x.GetRequiredActionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup.Actions.First());
    }

    public void AddActionGroupRetrieval(bool headingTo)
    {
        _actionGroupRetrievalService.Setup(x => x.IsAgvHeadingToNodeOfType(It.IsAny<Guid>(), It.IsAny<AgvGraph.NodeType>())).ReturnsAsync(headingTo);
    }

    public void AddChargingPointManager(GraphNode chargeGraphNode)
    {
        _chargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(chargeGraphNode);
    }

    public void AddParkingPointManager(GraphNode parkGraphNode)
    {
        _parkPointService.Setup(x => x.ReserveParkAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>())).ReturnsAsync(parkGraphNode);
    }

    public void AddRoute(string[] nodes)
    {
        _routeService.Setup(x => x.GetRouteAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Route(1f, nodes));
    }

    public void SetupValidationResult(List<ValidationFailure> failures)
    {
        _actionGroupValidationService.Setup(x => x.ValidateAsync(It.IsAny<ActionGroup>())).ReturnsAsync(failures);
    }

    public void IsActionGroupSaved(Times howOften)
    {
        _actionGroupRegistry.Verify(x => x.AddAsync(It.IsAny<ActionGroup>()), howOften);
    }

    public static IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(new ActionGroupProfile(), new AgvProfile());
        return mapper;
    }
}