using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Definitions;
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

    public Mock<IAgvActionRegistry> AgvActionRegistry { get; } = new();

    public Mock<IMaintenanceService> MaintenanceService { get; } = new();

    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    public Mock<IAgvStateService> AgvLocker { get; } = new();

    public Mock<IParkPointService> ParkPointManager { get; } = new();

    public Mock<IAgvLoadCapabilityService> AgvLoadCapabilityService { get; } = new();

    public Mock<IStartActionService> StartActionService { get; } = new();

    public Mock<IAgvService> AgvService { get; } = new();

    public Mock<IChooseAgvActionGroupService> ChooseAgvActionGroupService { get; } = new();

    public ActionManagerFixture()
    {
        AgvActionRegistry.Setup(x => x.GetNotStartedActionGroups()).Returns(_notStartedActionGroups);
        ChargingService.Setup(x => x.GetChargingAgvsWithGoodBatteryLevel()).Returns(_chargingAgvsWithGoodBatteryLevel);
        AgvRegistry.Setup(x => x.GetAllAgvs()).Returns(_agvs.AsQueryable());

        ActionManager = new ActionManager(
            AgvActionRegistry.Object,
            RouteService.Object,
            ChargingPointManager.Object,
            ParkPointManager.Object,
            MaintenanceService.Object,
            new Mock<ILogger<ActionManager>>().Object,
            ChargingService.Object,
            AgvRegistry.Object,
            AgvService.Object,
            AgvLoadCapabilityService.Object,
            StartActionService.Object,
            AgvLocker.Object,
            ChooseAgvActionGroupService.Object
        );
    }

    public ActionManagerFixture AddAssignedActionAndAgv(IAgv agv, AgvAction action)
    {
        action.AssignedAgv = agv.CorrelationId;
        AddAction(action);
        AddAgv(agv);
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private ActionManagerFixture AddAction(AgvAction action)
    {
        AgvActionRegistry.Setup(x => x.GetActionById(action.CorrelationId)).Returns(action);
        AgvActionRegistry.Setup(x => x.GetRequiredActionById(action.CorrelationId)).Returns(action);
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private ActionManagerFixture AddAgv(IAgv agv)
    {
        AgvRegistry.Setup(x => x.GetRequiredAgv(agv.CorrelationId)).Returns(agv);
        return this;
    }

    public ActionManagerFixture AddReservableChargingNode(AgvGraph.Node chargingNode)
    {
        ChargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<IAgv>())).Returns(Task.FromResult(chargingNode));
        return this;
    }

    public ActionManagerFixture AddReservableParkingNode(AgvGraph.Node parkingNode)
    {
        ParkPointManager.Setup(x => x.ReserveParkAsync(It.IsAny<IAgv>())).Returns(Task.FromResult(parkingNode));
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
        AgvLoadCapabilityService.Setup(m => m.IsCarryingLoad(agv.CorrelationId)).Returns(isCarryingLoad);
        return this;
    }

    public ActionManagerFixture WithAgvCanGetNewLoad(IAgv agv, bool canGetNewLoad = true)
    {
        AgvLoadCapabilityService.Setup(m => m.CanGetNewLoad(agv.CorrelationId)).Returns(canGetNewLoad);
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
        ParkPointManager.Setup(x => x.IsOnAParkNodeAsync(agv)).Returns(Task.FromResult(isOnParkNode));
        return this;
    }
    
    public ActionManagerFixture ChooseNextAgv(IAgv? agv, ActionGroup actionGroup)
    {
        ChooseAgvActionGroupService.Setup(x => x.GetAgvAsync(actionGroup, It.IsAny<IList<IAgv>>())).Returns(Task.FromResult(agv));  
        return this;
    }
    
    public ActionManagerFixture ChooseNextActionGroup(IAgv agv, ActionGroup actionGroup)
    {
        ChooseAgvActionGroupService.Setup(x => x.GetActionGroupAsync(agv)).Returns(Task.FromResult(actionGroup.Actions.FirstOrDefault()));
        return this;
    }

    public ActionManagerFixture AddNotStartedActionGroup(ActionGroup actionGroup)
    {
        _notStartedActionGroups.Add(actionGroup);
        return this;
    }
    
    public ActionManagerFixture SetSubsequentAction(IAgv agv, AgvAction subsequentAction)
    {
        AgvActionRegistry.Setup(x => x.GetSubsequentActionAsync(agv.CorrelationId)).Returns(Task.FromResult(subsequentAction));
        return this;
    }

    public ActionManagerFixture AddReadyAgv(IAgv agv, bool readyForLock = true, bool readyForLockWhenIgnoreChargingFlag = false)
    {
        if (readyForLock)
        {
            // if we are in general ready for lock then it does not matter what the ignoreChargingFlag is
            AgvService.Setup(x => x.IsAgvReadyForBusyAsync(agv, It.IsAny<bool>())).Returns(Task.FromResult(readyForLock)); 
        }
        else
        {
            AgvLocker.Setup(x => x.IsAgvReadyForBusyAsync(agv, false)).Returns(Task.FromResult(readyForLock));
            AgvLocker.Setup(x => x.IsAgvReadyForBusyAsync(agv, true)).Returns(Task.FromResult(readyForLockWhenIgnoreChargingFlag));
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

    public void VerifyActionStarted(AgvAction action) => StartActionService.Verify(sa => sa.StartAction(It.Is<AgvAction>(a => a == action), CancellationToken.None), Times.Once);

    public void VerifyNoActionStarted() => StartActionService.Verify(sa => sa.StartAction(It.IsAny<AgvAction>(), It.IsAny<CancellationToken>()), Times.Never);

    public void VerifyActionStarted(Expression<Func<AgvAction, bool>> filter) => StartActionService.Verify(sa => sa.StartAction(It.Is(filter), CancellationToken.None), Times.Once);

    public void VerifyActionStarted(AgvAction action, IAgv assignedAgv)
    {
        VerifyActionStarted(action);
        VerifyActionStartedWithAssignedAgv(assignedAgv);
    }

    public void VerifyActionStarted(Expression<Func<AgvAction, bool>> filter, IAgv assignedAgv)
    {
        VerifyActionStarted(filter);
        VerifyActionStartedWithAssignedAgv(assignedAgv);
    }

    private void VerifyActionStartedWithAssignedAgv(IAgv assignedAgv)
    {
        VerifyActionStarted(a => a.AssignedAgv == assignedAgv.CorrelationId &&
                                 a.AgvId == assignedAgv.AgvId);
    }
}