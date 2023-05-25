using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Maintenance;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.Extensions.Logging;
using Moq;

// ReSharper disable MemberCanBePrivate.Global

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

public class ActionManagerFixture
{
    private readonly List<ActionGroup> _notStartedActionGroups = new();
    private readonly List<IAgv> _chargingAgvsWithGoodBatteryLevel = new();
    private readonly List<IAgv> _agvs = new();

    public ActionManager ActionManager { get; private set; }

    public Mock<IChargingService> ChargingService { get; } = new();

    public Mock<IRouteService> RouteService { get; } = new();

    public Mock<IAgvRegistry> AgvRegistry { get; } = new();

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
        ActionGroupRegistry.Setup(x => x.GetNotStartedActionGroupsAsync()).ReturnsAsync(_notStartedActionGroups);
        ActionGroupRegistry.Setup(x => x.InsertAsync(It.IsAny<ActionGroup>())).Callback((ActionGroup actionGroup) => { AddActionGroup(actionGroup); });

        ChargingService.Setup(x => x.GetChargingAgvsWithGoodBatteryLevel()).Returns(_chargingAgvsWithGoodBatteryLevel);
        AgvRegistry.Setup(x => x.GetAllAgvs()).Returns(_agvs.AsQueryable());

        ActionManager = new ActionManager(
            ActionGroupRegistry.Object,
            RouteService.Object,
            ChargingPointManager.Object,
            ParkPointService.Object,
            MaintenanceService.Object,
            new Mock<ILogger<ActionManager>>().Object,
            ChargingService.Object,
            AgvRegistry.Object,
            AgvService.Object,
            AgvLoadCapabilityService.Object,
            StartActionService.Object,
            AgvStateService.Object,
            ChooseAgvActionGroupService.Object
        );
    }

    public ActionManagerFixture AddAssignedActionGroupAndAgv(ActionGroup actionGroup, IAgv agv, AgvAction? action = null)
    {
        actionGroup.AssignAgvToActionGroup(agv);
        if (action != null)
        {
            AddAction(action);
        }
        AddAgv(agv);
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
        AgvRegistry.Setup(x => x.GetRequiredAgv(agv.CorrelationId)).Returns(agv);
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private ActionManagerFixture AddActionGroup(ActionGroup actionGroup)
    {
        ActionGroupRegistry.Setup(x => x.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId)).ReturnsAsync(actionGroup);
        return this;
    }

    public ActionManagerFixture AddReservableChargingNode(AgvGraph.Node chargingNode)
    {
        ChargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<IAgv>())).ReturnsAsync(chargingNode);
        return this;
    }

    public ActionManagerFixture AddReservableParkingNode(AgvGraph.Node parkingNode)
    {
        ParkPointService.Setup(x => x.ReserveParkAsync(It.IsAny<IAgv>())).ReturnsAsync(parkingNode);
        return this;
    }

    public ActionManagerFixture WithAgvMaintenanceRequested(IAgv agv, bool maintenanceRequested = true, AgvGraph.Node? maintenanceNode = null)
    {
        if (maintenanceNode != null)
        {
            MaintenanceService.Setup(m => m.GetMaintenanceNode(agv)).Returns(maintenanceNode);
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
        RouteService.Setup(x => x.GetRoute(agv, It.IsAny<string>())).Returns(new IGraphAlgorithm.Route(1f, Array.Empty<string>()));
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
        ChooseAgvActionGroupService.Setup(x => x.GetActionGroupAsync(agv)).ReturnsAsync(actionGroup.Actions.FirstOrDefault());
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

    public ActionManagerFixture AddReadyAgv(IAgv agv, bool readyForLock = true, bool readyForLockWhenIgnoreChargingFlag = false)
    {
        if (readyForLock)
        {
            // if we are in general ready for lock then it does not matter what the ignoreChargingFlag is
            AgvService.Setup(x => x.IsAgvReadyForBusyAsync(agv, It.IsAny<bool>())).ReturnsAsync(readyForLock);
        }
        else
        {
            AgvStateService.Setup(x => x.IsAgvReadyForBusyAsync(agv, false)).ReturnsAsync(readyForLock);
            AgvStateService.Setup(x => x.IsAgvReadyForBusyAsync(agv, true)).ReturnsAsync(readyForLockWhenIgnoreChargingFlag);
        }

        SetupDefaultAgv(agv);
        return this;
    }

    private void SetupDefaultAgv(IAgv agv)
    {
        _agvs.Add(agv);

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