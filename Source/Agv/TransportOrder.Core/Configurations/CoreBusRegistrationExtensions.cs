using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Zones;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System.Monitoring;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Zone.Queries;
using MassTransit;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RemoveAgvFromZone = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RemoveAgvFromZone;
using RequestAgvData = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using RequestAgvZones = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvZones;
using ResendAction = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.ResendAction;
using ResetAgvErrors = Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands.ResetAgvErrors;
using StartInstantAction = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.StartInstantAction;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.Acc.TransportOrder.Core.Configurations;

/// <summary>
/// IBusRegistrationExtensions extensions
/// </summary>
public static class CoreBusRegistrationExtensions
{
    private const int PartitionCount = 16;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="busRegistrationConfigurator"></param>
    /// <returns></returns>
    public static IBusRegistrationConfigurator AddCoreConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddDelayedMessageScheduler();

        RegisterCoreActionGroupHandlers(busRegistrationConfigurator);

        RegisterCoreGraphHandlers(busRegistrationConfigurator);

        RegisterCoreAgvHandlers(busRegistrationConfigurator);

        RegisterTriggerHandlers(busRegistrationConfigurator);

        return busRegistrationConfigurator;
    }

    private static void RegisterCoreAgvHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<AgvDataEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvPositionReceivedEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvPositionChangedEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<CheckAgvLoad, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<AgvErrorChanged, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvStatus, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<LeaveQueue, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SetAgvGraphExist, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<GoOffline, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvData, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<AcceptAgvCommand, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SetMaintenanceMode, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SetUserMode, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SetNormalMode, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<LockAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UnlockAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SetAgvForceIdle, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<HandleAgvActionStartFailure, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<HandleActionCompletion, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<HandleActionAbortion, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<PrepareAgvForStartingAction, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<PrepareIdleActionOfAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<PrepareChargingOfAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvSettingsGroupReference, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SwitchMode, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<ReceiveAgvZones, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<RemoveAgvFromAllZones, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<RemoveAgvFromZone, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SwitchChargingAgvs, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<RemoveAgvFromZones, AgvCommandHandler>(c);
        });

        busRegistrationConfigurator.AddConsumer<ZoneCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<AddZone, ZoneCommandHandler>(c);
        });

        busRegistrationConfigurator.AddConsumer<AgvEventHandler>(c =>
            c.Message<AgvMapReceived>(o => o.UseMessageRetry(r =>
                {
                    // AgvMapReceived may be consumed before AgvGraph is stored in database.
                    r.Interval(3, TimeSpan.FromSeconds(5));
                    r.Handle<AgvGraphNotDefinedException>();
                }
            )));

        busRegistrationConfigurator.AddConsumer<InstantActionsCommandHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                partitioner.PartitionMessageById<AgvInstantActionDefinitionReceived, InstantActionsCommandHandler>(c);
            }
        );

        busRegistrationConfigurator.AddConsumer<MaintenanceLogCommandHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                partitioner.PartitionMessageById<AddMaintenanceLog, MaintenanceLogCommandHandler>(c);
            }
        );

        busRegistrationConfigurator.AddRequestClient<AcceptAgvCommand>();
        busRegistrationConfigurator.AddRequestClient<LockAgv>();
        busRegistrationConfigurator.AddRequestClient<SetAgvForceIdle>();

        busRegistrationConfigurator.AddConsumer<AgvErrorCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<UpdateErrorStatus, AgvErrorCommandHandler>(c);
            partitioner.PartitionMessageById<AddSystemAgvErrors, AgvErrorCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateSystemAgvErrors, AgvErrorCommandHandler>(c);
            partitioner.PartitionMessageById<ResolveSystemAgvError, AgvErrorCommandHandler>(c);
        });
    }

    private static void RegisterTriggerHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<TriggerEventHandler>();
        busRegistrationConfigurator.AddConsumer<TriggerCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<CreateTrigger, TriggerCommandHandler>(c);
            partitioner.PartitionMessageById<AddTriggerLocation, TriggerCommandHandler>(c);
            partitioner.PartitionMessageById<RemoveTriggerLocation, TriggerCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateDefaultConfigurationParameter, TriggerCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateLocationConfigurationParameter, TriggerCommandHandler>(c);
            partitioner.PartitionMessageById<ResetLocationConfigurationParameter, TriggerCommandHandler>(c);
        });
    }

    private static void RegisterCoreGraphHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<AgvGraphReceivedEventHandler>();
        busRegistrationConfigurator.AddConsumer<GetGraphRequestHandler>();
        busRegistrationConfigurator.AddRequestClient<GetGraph>();
        busRegistrationConfigurator.AddConsumer<GraphCommandHandler>();

        busRegistrationConfigurator.AddConsumer<StationEventHandler>();

        busRegistrationConfigurator.AddConsumer<StationCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<LockStation, StationCommandHandler>(c);
            partitioner.PartitionMessageById<UnlockStation, StationCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateStationJamState, StationCommandHandler>(c);
        });

        busRegistrationConfigurator.AddConsumer<NodeCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<SetNodeApproachingProperties>(x =>
                x.UsePartitioner(partitioner, p => p.Message.NodeId));
            c.Message<InitializeNodeApproachingProperties>(x =>
            {
                x.UsePartitioner(partitioner, p => p.Message.NodeId);

                x.UseMessageRetry(r =>
                {
                    r.Interval(6, TimeSpan.FromSeconds(5));
                    r.Handle<NodeNotFoundException>();
                });
            });
            var nodePartitioner = c.CreatePartitioner(PartitionCount);
            c.Message<FreeStationOccupancy>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.StationId));
            c.Message<OccupyStation>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.Station.Id));
            c.Message<StationReserved>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.StationId));
            c.Message<Common.Messages.Stations.UnblockStation>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.Id));
        });
        busRegistrationConfigurator.AddConsumer<MapCommandHandler>();

        busRegistrationConfigurator.AddConsumer<RestrictedAreaCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<SaveRestrictedArea, RestrictedAreaCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateRestrictedArea, RestrictedAreaCommandHandler>(c);
            partitioner.PartitionMessageById<DeleteRestrictedArea, RestrictedAreaCommandHandler>(c);
        });

        busRegistrationConfigurator.AddConsumer<QueueCommandHandler>();
        busRegistrationConfigurator.AddConsumer<SimulationCommandHandler>();
    }

    private static void RegisterCoreActionGroupHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<ActionEventHandler>();
        busRegistrationConfigurator.AddConsumer<ActionCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<TryStartNextActionInActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));
            c.Message<TryStartNewActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));

            var actionPartitioner = c.CreatePartitioner(PartitionCount);
            c.Message<CompleteActionOfActionGroup>(x =>
                {
                    x.UsePartitioner(actionPartitioner, p => p.Message.CorrelationId);

                    x.UseMessageRetry(r =>
                    {
                        // Retry once of the cargo has not been updated yet
                        // Used to compensate race conditions on events of updated messages 
                        r.Interval(3, TimeSpan.FromSeconds(5));
                        r.Handle<CargoStateMismatchException>();
                    });
                }
            );
            actionPartitioner.PartitionMessageById<CreateChargingActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<AbortActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<ResetActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<CreateActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<QueueActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<ForceQueueActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<EstimateActionGroupDuration, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<StartActionOfActionGroup, ActionCommandHandler>(c);

            actionPartitioner.PartitionMessageById<CreateQueueingActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<CreateParkingActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<SetTargetForAction, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<TryUpdateIdleAction, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<SaveActionGroupAbortion, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<ResumeActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<CancelOrAbortActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<ReassignActionGroup, ActionCommandHandler>(c);
        });
        busRegistrationConfigurator.AddRequestClient<CancelActionGroup>();
    }

    /// <summary>
    /// Add the mediator
    /// </summary>
    /// <param name="configurator">The registration configurator</param>
    public static void AddMediator(this IMediatorRegistrationConfigurator configurator)
    {
        RegisterMediatorMapHandlers(configurator);

        RegisterMediatorAgvRequestHandlers(configurator);

        RegisterMediatorRoutingRequestHandlers(configurator);

        RegisterMediatorAgvCommandHandlers(configurator);

        RegisterMediatorActionGroupHandlers(configurator);

        RegisterMediatorErrorHandlers(configurator);

        RegisterMediatorSystemHandlers(configurator);

        RegisterMediatorZoneHandlers(configurator);

        RegisterMediatorTriggerHandlers(configurator);

        RegisterMediatorRestrictedAreaHandlers(configurator);
    }

    private static void RegisterMediatorErrorHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<GetAgvErrorsRequestHandler>();
        configurator.AddRequestClient<GetAgvErrors>();
        configurator.AddRequestClient<GetActiveAgvErrors>();
        configurator.AddRequestClient<GetActiveErrorsForAgvs>();
        configurator.AddRequestClient<GetErrorHistoryForAgvs>();

        configurator.AddConsumer<ResetAgvErrorsRequestHandler>();
        configurator.AddRequestClient<ResetAgvErrors>();
    }

    private static void RegisterMediatorSystemHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<SystemRequestHandler>();
        configurator.AddRequestClient<GetSystemTime>();
        configurator.AddConsumer<FactoryTimeRequestHandler>();
        configurator.AddRequestClient<GetFactoryTimeZone>();
        configurator.AddRequestClient<UpdateHostOrderGeneration>();
        configurator.AddRequestClient<UpdateFactoryTimeZone>();

        configurator.AddRequestClient<GetCertificateExpirations>();
        configurator.AddConsumer<CertificateExpirationRequestHandler>();
    }

    private static void RegisterMediatorTriggerHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<TriggerRequestHandler>();
        configurator.AddRequestClient<GetTriggers>();
        configurator.AddRequestClient<AssignTriggerToLocation>();
        configurator.AddRequestClient<TryRemoveTriggerLocation>();
        configurator.AddRequestClient<GetTrigger>();
        configurator.AddRequestClient<TryUpdateDefaultConfigurationParameter>();
        configurator.AddRequestClient<TryUpdateLocationConfigurationParameter>();
        configurator.AddRequestClient<TryResetLocationConfigurationParameter>();
    }

    private static void RegisterMediatorZoneHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<ZoneRequestHandler>();
        configurator.AddRequestClient<GetZones>();
    }

    private static void RegisterMediatorActionGroupHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<ActionGroupRequestHandler>();
        configurator.AddRequestClient<GetActionGroup>();
        configurator.AddRequestClient<GetActionGroups>();
        configurator.AddRequestClient<GetActionGroupCountsByState>();
        configurator.AddRequestClient<GetHandlingUnits>();
        configurator.AddRequestClient<TriggerActionGroupAbortion>();
        configurator.AddRequestClient<TriggerActionGroupReset>();
        configurator.AddConsumer<CreateActionGroupRequestHandler>();
        configurator.AddRequestClient<CreatePickDropActionGroup>();
        configurator.AddConsumer<ActionCommandHandler>();
        configurator.AddRequestClient<QueueActionGroup>();
        configurator.AddRequestClient<TryReassignActionGroup>();
        configurator.AddRequestClient<TriggerActionCompletion>();
        configurator.AddRequestClient<TriggerActionGroupQueueing>();

        configurator.AddRequestClient<TriggerSetTarget>();
        configurator.AddRequestClient<GetActionGroupStatistics>();
        configurator.AddConsumer<ActionGroupStatisticsRequestHandler>();
    }

    private static void RegisterMediatorAgvCommandHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<EmitAgvRequestsHandler>();
        configurator.AddRequestClient<RequestAgvGraph>();
        configurator.AddRequestClient<RequestAgvData>();
        configurator.AddRequestClient<RequestAgvZones>();

        configurator.AddConsumer<AgvModeRequestHandler>();
        configurator.AddRequestClient<SetMaintenanceMode>();
        configurator.AddRequestClient<SetUserMode>();
        configurator.AddRequestClient<SetNormalMode>();

        configurator.AddConsumer<AgvZonesRequestHandler>();
        configurator.AddRequestClient<RemoveAgvFromAllZones>();
        configurator.AddRequestClient<RemoveAgvFromZone>();

        configurator.AddConsumer<AgvErrorRequestHandler>();
        configurator.AddRequestClient<SolveResourceBlockingErrors>();
    }

    private static void RegisterMediatorAgvRequestHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<GetAgvDataRequestHandler>();
        configurator.AddRequestClient<GetAgvData>();
        configurator.AddRequestClient<GetAgvsData>();

        configurator.AddConsumer<GetAgvBatteryRequestHandler>();
        configurator.AddRequestClient<GetAgvBattery>();
        configurator.AddRequestClient<GetAgvsBattery>();

        configurator.AddConsumer<GetCurrentAgvRouteRequestHandler>();
        configurator.AddRequestClient<GetCurrentAgvRoute>();
        configurator.AddRequestClient<GetCurrentAgvRoutes>();

        configurator.AddConsumer<GetGraphRequestHandler>();
        configurator.AddRequestClient<GetAgvGraph>();
        configurator.AddRequestClient<GetAgvGraphs>();
        configurator.AddRequestClient<GetMapGraph>();

        configurator.AddConsumer<GetAgvPositionsRequestHandler>();
        configurator.AddRequestClient<GetAgvPositions>();
        configurator.AddRequestClient<GetAgvPosition>();

        configurator.AddConsumer<MoveAgvRequestHandler>();
        configurator.AddRequestClient<MoveAgv>();
        configurator.AddRequestClient<MoveAgvToCoordinate>();
        configurator.AddRequestClient<ChargeAgv>();

        configurator.AddConsumer<GetAgvsRequestHandler>();
        configurator.AddRequestClient<GetAgvs>();
        configurator.AddRequestClient<GetAgv>();

        configurator.AddConsumer<CargoRequestHandler>();
        configurator.AddRequestClient<GetAgvHasCargo>();
        configurator.AddRequestClient<GetAgvsHasCargo>();
        configurator.AddRequestClient<GetCargosOfAgv>();
        configurator.AddRequestClient<GetCargosOfAgvs>();

        configurator.AddConsumer<AgvSettingsRequestHandler>();
        configurator.AddRequestClient<GetAgvSettings>();
        configurator.AddRequestClient<UpdateDefaultAgvSettings>();
        configurator.AddRequestClient<UpdateAgvSettings>();
        configurator.AddRequestClient<UpdateAgvsSettings>();
        configurator.AddRequestClient<GetAgvsSettings>();
        configurator.AddRequestClient<ResetAgvSettingToDefault>();
        configurator.AddRequestClient<ResetAgvsSettingToDefault>();

        configurator.AddConsumer<SendAgvToChargerRequestHandler>();
        configurator.AddRequestClient<SendAgvToCharger>();

        configurator.AddConsumer<InstantActionsRequestHandler>();
        configurator.AddRequestClient<GetAgvInstantActionDefinitions>();
        configurator.AddRequestClient<GetAgvsInstantActionsDefinitions>();
        configurator.AddRequestClient<StartInstantAction>();
        configurator.AddConsumer<ResendActionRequestHandler>();
        configurator.AddRequestClient<ResendAction>();

        configurator.AddConsumer<MaintenanceLogRequestHandler>();
        configurator.AddRequestClient<TriggerAddingMaintenanceLog>();
        configurator.AddRequestClient<GetMaintenanceLogsForAgv>();
        configurator.AddRequestClient<GetMaintenanceLogsForAgvs>();
    }

    private static void RegisterMediatorRoutingRequestHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<RouteRequestHandler>();
        configurator.AddRequestClient<GetRoute>();
    }

    private static void RegisterMediatorMapHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<NodeStateRequestHandler>();
        configurator.AddRequestClient<GetMapNodeStates>();

        configurator.AddConsumer<MapRequestHandler>();
        configurator.AddRequestClient<GetMapList>();
        configurator.AddRequestClient<GetMap>();
        configurator.AddRequestClient<GetBackgroundImage>();
        configurator.AddRequestClient<TrySettingNodeApproachingProperties>();

        configurator.AddConsumer<MapCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<SetBackground, MapCommandHandler>(c);
            partitioner.PartitionMessageById<DeleteBackground, MapCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateMap, MapCommandHandler>(c);
        });
        configurator.AddRequestClient<UpdateMap>();
        configurator.AddRequestClient<SetBackground>();
        configurator.AddRequestClient<DeleteBackground>();

        configurator.AddConsumer<StationsRequestHandler>();
        configurator.AddRequestClient<GetMapStations>();
        configurator.AddRequestClient<GetStations>();
        configurator.AddRequestClient<GetStation>();
        configurator.AddRequestClient<TryLockStation>();
        configurator.AddRequestClient<TryUnlockStation>();
        configurator.AddRequestClient<UnblockStation>();

        configurator.AddConsumer<AgvStationRequestHandler>();
        configurator.AddRequestClient<RemoveAgvFromStation>();

        configurator.AddConsumer<QueueRequestHandler>();
        configurator.AddRequestClient<GetQueues>();
        configurator.AddRequestClient<GetQueue>();
        configurator.AddRequestClient<TryAddQueue>();
        configurator.AddRequestClient<DeleteQueue>();
    }

    private static void RegisterMediatorRestrictedAreaHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<RestrictedAreaRequestHandler>();
        configurator.AddRequestClient<AddRestrictedArea>();
        configurator.AddRequestClient<TryDeleteRestrictedArea>();
        configurator.AddRequestClient<TryUpdateRestrictedArea>();
        configurator.AddRequestClient<GetRestrictedArea>();
        configurator.AddRequestClient<GetRestrictedAreas>();
    }
}