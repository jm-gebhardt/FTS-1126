using Gebhardt.StoreWare.Agv.Common.Extensions;
using Microsoft.Extensions.Logging;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Maintenance;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Assigns new actions based for multi LHD (loading handling devices)
/// </summary>
public class ActionManager : BaseActionManager
{
    /// <inheritdoc />
    public ActionManager(
        IAgvActionRegistry agvActionRegistry,
        IRouteService routeService,
        IChargingPointManager chargingPointManager,
        IParkPointService parkPointService,
        IMaintenanceService maintenanceService,
        ILogger<ActionManager> logger,
        IChargingService chargingService,
        IAgvRegistry agvRegistry,
        IAgvService agvService,
        IAgvLoadCapabilityService agvLoadCapabilityService,
        IStartActionService startActionService,
        IAgvStateService agvStateService,
        IChooseAgvActionGroupService chooseAgvActionGroupService
    )
        : base(agvActionRegistry,
            routeService,
            chargingPointManager,
            parkPointService,
            maintenanceService,
            logger,
            chargingService,
            agvRegistry,
            agvService,
            agvLoadCapabilityService,
            startActionService,
            agvStateService,
            chooseAgvActionGroupService
        )
    {
    }

    /// <inheritdoc />
    protected override async Task<AgvAction?> GetNextActionAsync(IAgv agv)
    {
        var nextAgvAction = await GetPendingActionAsync(agv) ?? await CreateIdleActionAsync(agv);
        return nextAgvAction;
    }

    private Task<AgvAction?> GetPendingActionAsync(IAgv agv)
    {
        bool needsCharging = ChargingService.AgvNeedsCharging(agv);
        bool isCarryingLoad = AgvLoadCapabilityService.IsCarryingLoad(agv.CorrelationId);
        bool canGetNewLoad = AgvLoadCapabilityService.CanGetNewLoad(agv.CorrelationId);
        bool needsMaintenance = MaintenanceService.MaintenanceRequested(agv);
        Logger.LogDebug("start GetPendingAction: needsCharging: {NeedsCharging}, isCarryingLoad: {IsCarryingLoad}, canGetNewLoad: {CanGetNewLoad}, needsMaintenance: {NeedsMaintenance}", needsCharging, canGetNewLoad, canGetNewLoad, needsMaintenance);

        // Always go directly to maintenance or charge, as this is prioritized (only if we are fully unloaded)
        if (!isCarryingLoad)
        {
            if (needsCharging)
            {
                return CreateChargingActionAsync(agv);
            }
            if (needsMaintenance)
            {
                return CreateMaintenanceActionAsync(agv)!;
            }
        }
        else if (needsCharging || needsMaintenance)
        {
            // we are loaded, and we want to get unloaded, so that we can do the charging or maintenance, therefore complete current action group
            return GetSucceedingActionAsync(agv)!;
        }

        // just get next action (complete any current ActionGroup or start a new ActionGroup)
        return GetNextActionAsync(agv, isCarryingLoad, canGetNewLoad);
    }

    private Task<AgvAction?> CreateIdleActionAsync(IAgv agv)
    {
        // if we can enhance the SOC, let's go charging
        if (ChargingService.AgvIsEligibleForCharging(agv))
        {
            return CreateChargingActionAsync(agv);
        }

        // otherwise do not block the road
        return CreateParkActionAsync(agv);
    }

    /// <summary>
    /// Determine what action to start depending on the isCarryingLoad/canGetNewLoad
    /// </summary>
    /// <param name="agv">The Agv to decide for</param>
    /// <param name="isCarryingLoad"></param>
    /// <param name="canGetNewLoad"></param>
    /// <returns></returns>
    // ReSharper disable once UnusedParameter.Local for future use
    private Task<AgvAction?> GetNextActionAsync(IAgv agv, bool isCarryingLoad, bool canGetNewLoad)
    {
        if (isCarryingLoad)
        {
            return GetSucceedingActionAsync(agv)!;
        }

        return GetNextActionGroupAsync(agv);
    }

    /// <inheritdoc />
    public override async Task CheckChargingAgvs(CancellationToken cancellationToken)
    {
        List<IAgv> goodAgvs = ChargingService.GetChargingAgvsWithGoodBatteryLevel();

        // preliminary logic: just go through all potential available Agvs and try to start a new action
        // later this should select only the best agv out of the goodAgvs list
        foreach (var agv in goodAgvs.OrderByDescending(a => a.GetRequiredAgvStatus().BatteryCharge))
        {
            var pendingAction = await GetPendingActionAsync(agv);

            if (pendingAction != null)
            {
                Logger.LogDebug("Trying to start a new action for charging Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
                await AssignExclusivelyAsync(agv, true, cancellationToken);
            }
        }
    }
}