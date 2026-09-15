using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.ActionGroups.Vehicle;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.HostOrder.Commands;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler.Metrics;
using Gebhardt.Acc.TransportOrder.Messages.Commands.BoundingBox;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Housekeeping;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Zones;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.AgvType;
using Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System.Monitoring;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Zone.Queries;
using MassTransit;
using DeleteBackground = Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image.DeleteBackground;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RemoveAgvFromZone = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RemoveAgvFromZone;
using RequestAgvData = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using RequestAgvZones = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvZones;
using ResendAction = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.ResendAction;
using ResetAgvErrors = Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands.ResetAgvErrors;
using StartInstantAction = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.StartInstantAction;
using UnblockStation = Gebhardt.Acc.Common.Messages.Stations.UnblockStation;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.Acc.TransportOrder.Core.Configurations;

/// <summary>
/// IBusRegistrationExtensions extensions
/// </summary>
public static class CoreBusRegistrationExtensions
{
    private const int PartitionCount = 16;

    /// <summary>
    /// Adds all the consumers for this project to the bus.
    /// </summary>
    /// <param name="busRegistrationConfigurator">The configurator</param>
    public static IBusRegistrationConfigurator AddCoreConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddDelayedMessageScheduler();

        RegisterCoreActionGroupHandlers(busRegistrationConfigurator);

        RegisterCoreGraphHandlers(busRegistrationConfigurator);

        RegisterCoreAgvHandlers(busRegistrationConfigurator);

        RegisterTriggerHandlers(busRegistrationConfigurator);

        busRegistrationConfigurator.AddConsumer<MetricsCollectionEventHandler>();
        busRegistrationConfigurator.AddConsumer<ActionStartLatencyEventHandler>();
        busRegistrationConfigurator.AddConsumer<LogEntryEventHandler>();
        busRegistrationConfigurator.AddConsumer<LogEntryCommandHandler>();
        busRegistrationConfigurator.AddConsumer<NewGraphCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(1);
            partitioner.PartitionMessageById<AddNewGraphSegment, NewGraphCommandHandler>(c);
        });

        return busRegistrationConfigurator;
    }

    private static void RegisterCoreAgvHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<AgvDataEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvTypeEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvTypeCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<AddOrUpdateAgvType, AgvTypeCommandHandler>(c);
            partitioner.PartitionMessageById<AcknowledgeAgvType, AgvTypeCommandHandler>(c);
        });
        busRegistrationConfigurator.AddConsumer<AgvPositionReceivedEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvPositionChangedEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvStatusRequestHandler>();
        busRegistrationConfigurator.AddConsumer<AgvCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<CheckAgvLoad, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<AgvErrorChanged, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvStatus, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<LeaveQueue, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SetAgvGraphExist, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<GoOffline, AgvCommandHandler>(c);

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
            partitioner.PartitionMessageById<UpdateCargo, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvSettingsGroupReference, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SwitchMode, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<ReceiveAgvZones, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<RemoveAgvFromAllZones, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<RemoveAgvFromZone, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SwitchChargingAgvs, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<RemoveAgvFromZones, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<RemoveAgvFromQueue, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<RefreshAgvInformation, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<AcknowledgeAgvTypeChange, AgvCommandHandler>(c);

            c.Message<UpdateAgvData>(x =>
            {
                x.UsePartitioner(partitioner, p => p.Message.CorrelationId);
                x.UseMessageRetry(r =>
                {
                    r.Interval(6, TimeSpan.FromSeconds(5));
                    r.Handle<AgvNotFoundException>();
                });
            });
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
            partitioner.PartitionMessageById<MarkTriggerForDeletion, TriggerCommandHandler>(c);
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
        busRegistrationConfigurator.AddConsumer<StationTypeRequestHandler>();
        busRegistrationConfigurator.AddConsumer<BoundingBoxEventHandler>();
        busRegistrationConfigurator.AddConsumer<NewGraphEventHandler>();
        busRegistrationConfigurator.AddConsumer<BoundingBoxCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(1);
            partitioner.PartitionMessageById<CalculateBoundingBoxes, BoundingBoxCommandHandler>(c);
            partitioner.PartitionMessageById<CalculateBoundingBoxesForAgvType, BoundingBoxCommandHandler>(c);
        });
        busRegistrationConfigurator.AddConsumer<NodeStationEventHandler>();
        busRegistrationConfigurator.AddConsumer<QueueEventHandler>();

        busRegistrationConfigurator.AddConsumer<NodeStationCommandHandler>(c =>
        {
            var nodePartitioner = c.CreatePartitioner(PartitionCount);

            c.Message<SetNodeApproachingProperties>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.NodeId));
            c.Message<InitializeNodeApproachingProperties>(x =>
            {
                x.UsePartitioner(nodePartitioner, p => p.Message.NodeId);

                x.UseMessageRetry(r =>
                {
                    r.Interval(6, TimeSpan.FromSeconds(5));
                    r.Handle<NodeNotFoundException>();
                });
            });
            c.Message<FreeNode>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.NodeId));
            c.Message<UpdateNodeOccupancy>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.OldLastNodeId.GetValueOrDefault()));
            c.Message<FreeStationOccupancy>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.NodeId));
            c.Message<ReserveStation>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.Station.Id));
            c.Message<UnblockStation>(x =>
                x.UsePartitioner(nodePartitioner, p => p.Message.StationId));
            nodePartitioner.PartitionMessageById<LockStation, NodeStationCommandHandler>(c);
            nodePartitioner.PartitionMessageById<UnlockStation, NodeStationCommandHandler>(c);
            nodePartitioner.PartitionMessageById<UpdateStationJamState, NodeStationCommandHandler>(c);
            nodePartitioner.PartitionMessageById<UpdateStationIsOnline, NodeStationCommandHandler>(c);
            nodePartitioner.PartitionMessageById<StationConfigurationReceived, NodeStationCommandHandler>(c);
            c.Message<UpdateStationAdditionalInfo>(x =>
            {
                x.UsePartitioner(nodePartitioner, p => p.Message.CorrelationId);

                x.UseMessageRetry(r =>
                {
                    r.Interval(6, TimeSpan.FromSeconds(7));
                    r.Handle<NodeNotFoundException>();
                });
            });
        });

        busRegistrationConfigurator.AddConsumer<MapCommandHandler>();

        busRegistrationConfigurator.AddConsumer<RestrictedAreaCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<SaveRestrictedArea, RestrictedAreaCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateRestrictedArea, RestrictedAreaCommandHandler>(c);
            partitioner.PartitionMessageById<DeleteRestrictedArea, RestrictedAreaCommandHandler>(c);
        });

        busRegistrationConfigurator.AddConsumer<QueueCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<AdjustAgvFillQuantity>(x =>
                x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
        });

        busRegistrationConfigurator.AddConsumer<SimulationCommandHandler>();
    }

    private static void RegisterCoreActionGroupHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<TargetSelectionCommandHandler>();
        busRegistrationConfigurator.AddRequestClient<RetractHostOrder>();
        busRegistrationConfigurator.AddConsumer<ActionEventHandler>();
        busRegistrationConfigurator.AddConsumer<ActionCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<TryStartNextActionInActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));
            c.Message<TryStartNewActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));
            c.Message<EstimateActionGroupDuration>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));

            var actionPartitioner = c.CreatePartitioner(PartitionCount);
            c.Message<CompleteActionOfActionGroup>(x =>
                {
                    x.UsePartitioner(actionPartitioner, p => p.Message.CorrelationId);

                    x.UseMessageRetry(r =>
                    {
                        // Retry once of the cargo has not been updated yet
                        // Used to compensate race conditions on events of updated messages 
                        r.Interval(6, TimeSpan.FromSeconds(5));
                        r.Handle<CargoStateMismatchException>();
                    });
                }
            );
            actionPartitioner.PartitionMessageById<CreateChargingActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<AbortActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<HandleActionFailure, ActionCommandHandler>(c);
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
            actionPartitioner.PartitionMessageById<CancelActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<ReassignActionGroup, ActionCommandHandler>(c);
        });
        busRegistrationConfigurator.AddConsumer<DoppelspielCommandHandler>();
        busRegistrationConfigurator.AddRequestClient<CancelActionGroup>();

        busRegistrationConfigurator.AddConsumer<ActionDispatchConsumer>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<StartActionCommand, ActionDispatchConsumer>(c);
            partitioner.PartitionMessageById<UpdateActionCommand, ActionDispatchConsumer>(c);
            partitioner.PartitionMessageById<AbortAndStartActionCommand, ActionDispatchConsumer>(c);
            partitioner.PartitionMessageById<Common.Messages.ActionGroups.Commands.ResendAction, ActionDispatchConsumer>(c);
        });

        busRegistrationConfigurator.AddConsumer<NewBaseRequestConsumer>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<AgvNewBaseRequested, NewBaseRequestConsumer>(c);
        });

        busRegistrationConfigurator.AddConsumer<RoutePlanCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<DispatchRouteCommand, RoutePlanCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateDispatchedRouteCommand, RoutePlanCommandHandler>(c);
            partitioner.PartitionMessageById<AbortAndDispatchRouteCommand, RoutePlanCommandHandler>(c);
            partitioner.PartitionMessageById<ResendDispatchedRouteCommand, RoutePlanCommandHandler>(c);
            c.Message<ActionGroupCompleted>(x =>
                x.UsePartitioner(partitioner, p => p.Message.ActionGroup.AssignedAgv.GetValueOrDefault()));
        });
    }

    /// <summary>
    /// Add the mediator
    /// </summary>
    /// <param name="configurator">The registration configurator</param>
    public static void AddMediator(this IMediatorRegistrationConfigurator configurator)
    {
        RegisterMediatorGraphHandlers(configurator);

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

        RegisterMediatorBackgroundEditorHandlers(configurator);

        RegisterMediatorLogRequestHandlers(configurator);

        RegisterMediatorOwnedResourceHandlers(configurator);

        RegisterMediatorAgvHandlers(configurator);
    }

    private static void RegisterMediatorGraphHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<BoundingBoxRequestHandler>();
        configurator.AddRequestClient<GetBoundingBoxes>();

        configurator.AddConsumer<BoundingBoxConflictRequestHandler>();
        configurator.AddRequestClient<GetBoundingBoxConflicts>();
        configurator.AddRequestClient<GetNodeBoundingBoxConflicts>();
        configurator.AddRequestClient<GetEdgeBoundingBoxConflicts>();
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

        configurator.AddRequestClient<UpdateHousekeepingConfiguration>();
        configurator.AddConsumer<HousekeepingRequestHandler>();
    }

    private static void RegisterMediatorTriggerHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<TriggerRequestHandler>();
        configurator.AddRequestClient<GetTriggers>();
        configurator.AddRequestClient<GetDeletedTriggers>();
        configurator.AddRequestClient<TryMarkTriggerForDeletion>();
        configurator.AddRequestClient<TryRestoreTrigger>();
        configurator.AddRequestClient<AssignTriggerToLocation>();
        configurator.AddRequestClient<TryRemoveTriggerLocation>();
        configurator.AddRequestClient<GetTrigger>();
        configurator.AddRequestClient<TryUpdateDefaultConfigurationParameter>();
        configurator.AddRequestClient<TryUpdateLocationConfigurationParameter>();
        configurator.AddRequestClient<TryResetLocationConfigurationParameter>();
        configurator.AddRequestClient<DuplicateTriggerLocations>();
        configurator.AddRequestClient<DuplicateTriggerLocation>();
    }

    private static void RegisterMediatorZoneHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<ZoneRequestHandler>();
        configurator.AddRequestClient<GetZones>();
    }

    private static void RegisterMediatorAgvHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<AgvTypeRequestHandler>();
        configurator.AddRequestClient<RequestAgvTypeAcknowledgement>();
        configurator.AddRequestClient<GetAgvType>();
        configurator.AddRequestClient<GetAllAgvTypes>();
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
        configurator.AddRequestClient<GetFilteredActionGroups>();
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

        configurator.AddRequestClient<RequestAgvTypeChangeAcknowledgement>();
    }

    private static void RegisterMediatorRoutingRequestHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<RouteRequestHandler>();
        configurator.AddRequestClient<GetRoute>();
    }

    private static void RegisterMediatorLogRequestHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<LogEntryRequestHandler>();
        configurator.AddRequestClient<GetFilteredLogEntries>();
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
        configurator.AddRequestClient<Gebhardt.Acc.TransportOrder.Messages.Requests.Stations.UnblockStation>();
        configurator.AddRequestClient<SendJamStateRequest>();

        configurator.AddConsumer<AgvStationRequestHandler>();
        configurator.AddRequestClient<RemoveAgvFromStation>();

        configurator.AddConsumer<QueueRequestHandler>();
        configurator.AddRequestClient<GetQueues>();
        configurator.AddRequestClient<GetQueue>();
        configurator.AddRequestClient<TryAddQueue>();
        configurator.AddRequestClient<DeleteQueue>();
        configurator.AddRequestClient<RemoveAgvFromQueue>();
        configurator.AddRequestClient<AdjustAgvFillQuantity>();
    }

    private static void RegisterMediatorBackgroundEditorHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<BackgroundEditorRequestHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<AddOrUpdateBackground, BackgroundEditorRequestHandler>(c);
            partitioner.PartitionMessageById<DeleteBackgroundScene, BackgroundEditorRequestHandler>(c);
        });
        configurator.AddRequestClient<AddOrUpdateBackground>();
        configurator.AddRequestClient<GetBackground>();
        configurator.AddRequestClient<GetBackgrounds>();
        configurator.AddRequestClient<DeleteBackgroundScene>();
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

    private static void RegisterMediatorOwnedResourceHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<OwnedFilterRequestHandler>();
        configurator.AddRequestClient<GetOwnedFilters>();
        configurator.AddRequestClient<AddOwnedFilter>();
        configurator.AddRequestClient<UpdateOwnedFilter>();
        configurator.AddRequestClient<DeleteOwnedFilter>();

        configurator.AddConsumer<FilterTemplateRequestHandler>();
        configurator.AddRequestClient<GetFilterTemplates>();
        configurator.AddRequestClient<AddFilterTemplate>();
        configurator.AddRequestClient<UpdateFilterTemplate>();
        configurator.AddRequestClient<DeleteFilterTemplate>();
    }
}