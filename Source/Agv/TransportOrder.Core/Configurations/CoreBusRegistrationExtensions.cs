using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.Triggers.Commands;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.InstantActions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using MassTransit;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using ResetAgvErrors = Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands.ResetAgvErrors;
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
            partitioner.PartitionMessageById<UpdateErrorStatus, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvStatus, AgvCommandHandler>(c);
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
            partitioner.PartitionMessageById<PrepareAgvForStartingAction, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<PrepareParkingOfAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<PrepareChargingOfAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvSettingsGroupReference, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SwitchMode, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<TryReachQueueHead, AgvCommandHandler>(c);
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

        busRegistrationConfigurator.AddRequestClient<AcceptAgvCommand>();
        busRegistrationConfigurator.AddRequestClient<LockAgv>();
        busRegistrationConfigurator.AddRequestClient<SetAgvForceIdle>();

        busRegistrationConfigurator.AddConsumer<AgvErrorCommandHandler>();
        busRegistrationConfigurator.AddRequestClient<UpdateErrorStatus>();
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
        busRegistrationConfigurator.AddConsumer<GraphEventHandler>();

        busRegistrationConfigurator.AddConsumer<StationCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<LockStation, StationCommandHandler>(c);
            partitioner.PartitionMessageById<UnlockStation, StationCommandHandler>(c);
        });

        busRegistrationConfigurator.AddConsumer<NodeCommandHandler>();
        busRegistrationConfigurator.AddConsumer<MapCommandHandler>();

        busRegistrationConfigurator.AddConsumer<RestrictedAreaCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<SaveRestrictedArea, RestrictedAreaCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateRestrictedArea, RestrictedAreaCommandHandler>(c);
            partitioner.PartitionMessageById<DeleteRestrictedArea, RestrictedAreaCommandHandler>(c);
        });

        busRegistrationConfigurator.AddConsumer<QueueCommandHandler>();
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
            actionPartitioner.PartitionMessageById<CreateChargingActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<AbortActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<ResetActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<QueueActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<EstimateActionGroupDuration, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<StartActionOfActionGroup, ActionCommandHandler>(c);
            actionPartitioner.PartitionMessageById<CompleteActionOfActionGroup, ActionCommandHandler>(c);
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
        configurator.AddRequestClient<UpdateFactoryTimeZone>();
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

    private static void RegisterMediatorActionGroupHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<ActionGroupRequestHandler>();
        configurator.AddRequestClient<GetActionGroup>();
        configurator.AddRequestClient<GetActionGroups>();
        configurator.AddRequestClient<GetActionGroupCountsByState>();
        configurator.AddRequestClient<TriggerActionGroupAbortion>();
        configurator.AddRequestClient<TriggerActionGroupReset>();
        configurator.AddConsumer<CreateActionGroupRequestHandler>();
        configurator.AddRequestClient<CreatePickDropActionGroup>();
        configurator.AddConsumer<ActionCommandHandler>();
        configurator.AddRequestClient<QueueActionGroup>();
        configurator.AddRequestClient<TryReassignActionGroup>();

        configurator.AddRequestClient<GetActionGroupStatistics>();
        configurator.AddConsumer<ActionGroupStatisticsRequestHandler>();
    }

    private static void RegisterMediatorAgvCommandHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<RequestAgvDataCommandHandler>();
        configurator.AddRequestClient<RequestAgvData>();

        configurator.AddConsumer<RequestAgvGraphCommandHandler>();
        configurator.AddRequestClient<RequestAgvGraph>();

        configurator.AddConsumer<AgvModeRequestHandler>();
        configurator.AddRequestClient<SetMaintenanceMode>();
        configurator.AddRequestClient<SetUserMode>();
        configurator.AddRequestClient<SetNormalMode>();
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
    }

    private static void RegisterMediatorRoutingRequestHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<RouteRequestHandler>();
        configurator.AddRequestClient<GetRoute>();
    }

    private static void RegisterMediatorMapHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<GetMapImageRequestHandler>();
        configurator.AddRequestClient<GetMapImage>();

        configurator.AddConsumer<NodeStateRequestHandler>();
        configurator.AddRequestClient<GetMapNodeStates>();

        configurator.AddConsumer<GetMapRequestHandler>();
        configurator.AddRequestClient<GetMapList>();
        configurator.AddRequestClient<GetMap>();

        configurator.AddConsumer<StationsRequestHandler>();
        configurator.AddRequestClient<GetMapStations>();
        configurator.AddRequestClient<GetStations>();
        configurator.AddRequestClient<GetStation>();
        configurator.AddRequestClient<TryLockStation>();
        configurator.AddRequestClient<TryUnlockStation>();

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
