using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Extensions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Maintenance;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

// ReSharper disable MemberCanBePrivate.Global

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

public class ActionManagerFixture
{
    private readonly Dictionary<Guid, ActionGroup> _actionGroups = new();
    private readonly List<ActionGroup> _notStartedActionGroups = new();
    private readonly List<IAgv> _chargingAgvsWithGoodBatteryLevel = new();

    public ActionManager ActionManager { get; private set; }

    public Mock<IBus> Bus { get; } = new();

    public Mock<IChargingService> ChargingService { get; } = new();

    public Mock<IRouteService> RouteService { get; } = new();

    public Mock<IMaintenanceService> MaintenanceService { get; } = new();

    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    public Mock<IParkPointService> ParkPointService { get; } = new();

    public Mock<IAgvLoadCapabilityService> AgvLoadCapabilityService { get; } = new();

    public Mock<IStartActionService> StartActionService { get; } = new();

    public Mock<IAgvService> AgvService { get; } = new();

    public Mock<IChooseAgvActionGroupService> ChooseAgvActionGroupService { get; } = new();

    public Mock<IActionGroupRegistry> ActionGroupRegistry { get; } = new();

    public Mock<IAgvStateService> AgvStateService { get; } = new();

    public ActionManagerFixture()
    {
        ActionGroupRegistry.Setup(x => x.GetNotStartedActionGroupsAsync(It.IsAny<Guid>())).ReturnsAsync(_notStartedActionGroups);
        ActionGroupRegistry.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).Callback((ActionGroup actionGroup) => { AddActionGroup(actionGroup); });
        ActionGroupRegistry.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid correlationId) => _actionGroups[correlationId]);
        ActionGroupRegistry.Setup(x => x.UpdateActionAndGroupStateAsync(It.IsAny<AgvAction>(), It.IsAny<IAgv>(), It.IsAny<ActionState>()))
            .ReturnsAsync((AgvAction agvAction, IAgv _, ActionState _) =>
            {
                var actionGroup = _actionGroups[agvAction.ParentId];
                actionGroup.UpdateActionGroupState();
                return actionGroup;
            });
        ActionGroupRegistry.Setup(x => x.AddAsync(It.IsAny<ActionGroup>())).Callback((ActionGroup actionGroup) => { AddActionGroup(actionGroup); });
        ActionGroupRegistry.Setup(x => x.GetActionGroupsAsync(ActionGroupState.Created, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new List<ActionGroup>());
        ChargingService.Setup(x => x.GetChargingAgvsWithGoodBatteryLevel()).Returns(_chargingAgvsWithGoodBatteryLevel);

        ActionManager = new ActionManager(
            Bus.Object,
            ActionGroupRegistry.Object,
            RouteService.Object,
            ChargingPointManager.Object,
            ParkPointService.Object,
            MaintenanceService.Object,
            new Mock<ILogger<ActionManager>>().Object,
            ChargingService.Object,
            AgvService.Object,
            AgvLoadCapabilityService.Object,
            StartActionService.Object,
            AgvStateService.Object,
            ChooseAgvActionGroupService.Object
        );
    }

    public ActionManagerFixture AddAssignedActionGroupAndAgv(ActionGroup actionGroup, IAgv agv, AgvAction? action = null)
    {
        actionGroup.Assign(agv);
        if (action != null)
        {
            AddAction(action);
        }

        AddActionGroup(actionGroup);

        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private ActionManagerFixture AddAction(AgvAction action)
    {
        ActionGroupRegistry.Setup(x => x.GetActionByIdAsync(action.CorrelationId)).ReturnsAsync(action);
        ActionGroupRegistry.Setup(x => x.GetRequiredActionByIdAsync(action.CorrelationId)).ReturnsAsync(action);
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private ActionManagerFixture AddAgv(IAgv agv)
    {
        AgvService.Setup(x => x.GetAgvAsync(agv.CorrelationId, It.IsAny<CancellationToken>())).ReturnsAsync(agv);
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private ActionManagerFixture AddActionGroup(ActionGroup actionGroup)
    {
        ActionGroupRegistry.Setup(x => x.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId)).ReturnsAsync(actionGroup);
        if (actionGroup.State == ActionGroupState.Created)
        {
            _notStartedActionGroups.Add(actionGroup);
        }
        _actionGroups[actionGroup.CorrelationId] = actionGroup;
        return this;
    }

    public ActionManagerFixture AddManuallyTriggeredActionGroup(IAgv agv, ActionGroup actionGroup)
    {
        ActionGroupRegistry.Setup(x => x.GetActionGroupsAsync(ActionGroupState.Created,It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), agv.CorrelationId, true))
            .ReturnsAsync(new List<ActionGroup> { actionGroup });
        
        if (actionGroup.State == ActionGroupState.Created)
        {
            _notStartedActionGroups.Add(actionGroup);
        }
        _actionGroups[actionGroup.CorrelationId] = actionGroup;
        return this;
    }

    public ActionManagerFixture AddReservableChargingNode(GraphNode chargingNode)
    {
        ChargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<IAgv>())).ReturnsAsync(chargingNode);
        return this;
    }

    public ActionManagerFixture AddReservableParkingNode(GraphNode parkingNode)
    {
        ParkPointService.Setup(x => x.ReserveParkAsync(It.IsAny<IAgv>())).ReturnsAsync(parkingNode);
        return this;
    }

    public ActionManagerFixture WithAgvMaintenanceRequested(IAgv agv, bool maintenanceRequested = true, GraphNode? maintenanceNode = null)
    {
        if (maintenanceNode != null)
        {
            MaintenanceService.Setup(m => m.GetMaintenanceNode(agv)).ReturnsAsync(maintenanceNode);
        }
        MaintenanceService.Setup(m => m.MaintenanceRequested(agv)).Returns(maintenanceRequested);
        return this;
    }

    public ActionManagerFixture WithAgvNeedsCharging(IAgv agv, bool needsCharging = true)
    {
        ChargingService.Setup(m => m.AgvNeedsCharging(agv)).Returns(needsCharging);
        return this;
    }

    public ActionManagerFixture WithAgvEligibleForCharging(IAgv agv, bool eligibleForCharging = true)
    {
        ChargingService.Setup(m => m.AgvIsEligibleForCharging(agv)).Returns(eligibleForCharging);
        return this;
    }

    public ActionManagerFixture WithAgvCarryingLoad(IAgv agv, bool isCarryingLoad = true)
    {
        AgvLoadCapabilityService.Setup(m => m.IsCarryingLoadAsync(agv.CorrelationId)).ReturnsAsync(isCarryingLoad);
        return this;
    }

    public ActionManagerFixture WithAgvCanGetNewLoad(IAgv agv, bool canGetNewLoad = true)
    {
        AgvLoadCapabilityService.Setup(m => m.CanGetNewLoadAsync(agv.CorrelationId)).ReturnsAsync(canGetNewLoad);
        return this;
    }

    public ActionManagerFixture WithAgvHasRouteToDestination(IAgv agv, bool hasRoute = true)
    {
        float? routeCosts;
        RouteService.Setup(x => x.HasRoute(agv, It.IsAny<string>(), out routeCosts)).Returns(hasRoute);
        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>())).ReturnsAsync(new IGraphAlgorithm.Route(1f, Array.Empty<string>()));
        return this;
    }

    public ActionManagerFixture WithAgvOnAParkNode(IAgv agv, bool isOnParkNode = true)
    {
        ParkPointService.Setup(x => x.IsOnAParkNodeAsync(agv)).ReturnsAsync(isOnParkNode);
        return this;
    }

    public ActionManagerFixture ChooseNextAgv(IAgv? agv, ActionGroup actionGroup)
    {
        ChooseAgvActionGroupService.Setup(x => x.GetAgvAsync(actionGroup, It.IsAny<IList<IAgv>>())).ReturnsAsync(agv);
        return this;
    }

    public ActionManagerFixture ChooseNextActionGroup(IAgv agv, ActionGroup actionGroup)
    {
        ChooseAgvActionGroupService.Setup(x => x.GetNotStartedActionGroupAsync(agv)).ReturnsAsync(actionGroup);
        return this;
    }

    public ActionManagerFixture AddNotStartedActionGroup(ActionGroup actionGroup)
    {
        _notStartedActionGroups.Add(actionGroup);
        AddActionGroup(actionGroup);

        return this;
    }

    public ActionManagerFixture SetSubsequentAction(IAgv agv, AgvAction subsequentAction)
    {
        AddAction(subsequentAction);
        ActionGroupRegistry.Setup(x => x.GetSubsequentActionAsync(agv.CorrelationId)).ReturnsAsync(subsequentAction);
        return this;
    }

    public ActionManagerFixture AddReadyAgv(IAgv agv, bool readyForBusy = true, bool readyForBusyWhenIgnoreChargingFlag = false)
    {
        if (readyForBusy)
        {
            // if we are in general ready for lock then it does not matter what the ignoreChargingFlag is
            AgvService.Setup(x => x.IsAgvReadyForBusyAsync(agv.CorrelationId, It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(readyForBusy);
        }
        else
        {
            AgvStateService.Setup(x => x.IsAgvReadyForBusyAsync(agv.CorrelationId, It.IsAny<CancellationToken>(), false, false)).ReturnsAsync(readyForBusy);
            AgvStateService.Setup(x => x.IsAgvReadyForBusyAsync(agv.CorrelationId, It.IsAny<CancellationToken>(), false, true)).ReturnsAsync(readyForBusyWhenIgnoreChargingFlag);
        }

        SetupDefaultAgv(agv);
        AddAgv(agv);
        return this;
    }

    private void SetupDefaultAgv(IAgv agv)
    {
        // By default everything is deactivated, the caller has to set it externally if he needs it
        WithAgvMaintenanceRequested(agv, false);
        WithAgvNeedsCharging(agv, false);
        WithAgvEligibleForCharging(agv, false);
        WithAgvCarryingLoad(agv, false);
        WithAgvCanGetNewLoad(agv, false);
        WithAgvHasRouteToDestination(agv, false);
        WithAgvOnAParkNode(agv, false);
    }

    public void VerifyActionStarted(IAgv assignedAgv, AgvAction action) => StartActionService.Verify(sa => sa.StartAction(assignedAgv, action, It.IsAny<CancellationToken>()), Times.Once);

    public void VerifyNoActionStarted() => StartActionService.Verify(sa => sa.StartAction(It.IsAny<IAgv>(), It.IsAny<AgvAction>(), It.IsAny<CancellationToken>()), Times.Never);

    public void VerifyActionStarted(Expression<Func<IAgv, bool>> filterAgv, Expression<Func<AgvAction, bool>> filterAction) => StartActionService.Verify(sa => sa.StartAction(It.Is(filterAgv), It.Is(filterAction), It.IsAny<CancellationToken>()), Times.Once);
}