using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Routing;
using MassTransit;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;
using ResetAgvErrors = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands.ResetAgvErrors;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;

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

        return busRegistrationConfigurator;
    }

    private static void RegisterCoreAgvHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<AgvDataEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvPositionReceivedEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<UpdateErrorStatus, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<StartCharging, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<StopCharging, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvStatus, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateGraph, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<GoOffline, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UpdateAgvData, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<AcceptAgvCommand, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<LockAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<UnlockAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<MoveAgv, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<SetAgvForceIdle, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<HandleAgvActionStartFailure, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<HandleActionCompletion, AgvCommandHandler>(c);
            partitioner.PartitionMessageById<PrepareAgvForStartingAction, AgvCommandHandler>(c);
        });
        busRegistrationConfigurator.AddConsumer<AgvEventHandler>();

        busRegistrationConfigurator.AddRequestClient<AcceptAgvCommand>();
        busRegistrationConfigurator.AddRequestClient<LockAgv>();
        busRegistrationConfigurator.AddRequestClient<UnlockAgv>();
        busRegistrationConfigurator.AddRequestClient<MoveAgv>();
        busRegistrationConfigurator.AddRequestClient<SetAgvForceIdle>();

        busRegistrationConfigurator.AddConsumer<ResetAgvErrorsRequestHandler>();
        busRegistrationConfigurator.AddRequestClient<ResetAgvErrors>();

        busRegistrationConfigurator.AddConsumer<AgvErrorCommandHandler>();
        busRegistrationConfigurator.AddRequestClient<UpdateErrorStatus>();
    }

    private static void RegisterCoreGraphHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<AgvGraphReceivedEventHandler>();

        busRegistrationConfigurator.AddConsumer<NodeCommandHandler>();

        busRegistrationConfigurator.AddConsumer<MapCommandHandler>(cfg => { cfg.Message<SaveMap>(x => x.UsePartitioner(PartitionCount, p => p.Message.Map.CorrelationId)); });
    }

    private static void RegisterCoreActionGroupHandlers(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddConsumer<ActionEventHandler>();
        busRegistrationConfigurator.AddConsumer<ActionCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<CreateParkingActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
            c.Message<CreateChargingActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));
            c.Message<CreateMaintenanceActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));
            c.Message<TryStartNextActionInActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));
            c.Message<TryStartNewActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));
            c.Message<TryStartManualAction>(x =>
                x.UsePartitioner(partitioner, p => p.Message.AgvCorrelationId));
        });
        busRegistrationConfigurator.AddRequestClient<CancelActionGroup>();
    }

    /// <summary>
    /// Adds the MassTransit Mediator
    /// MassTransit Mediator runs in-process and in-memory, no transport is required.
    /// </summary>
    /// <param name="configurator"></param>
    public static void AddMediator(this IMediatorRegistrationConfigurator configurator)
    {
        RegisterMediatorMapHandlers(configurator);

        RegisterMediatorAgvRequestHandlers(configurator);

        RegisterMediatorRoutingRequestHandlers(configurator);

        RegisterMediatorAgvCommandHandlers(configurator);

        RegisterMediatorActionGroupHandlers(configurator);

        RegisterMediatorErrorHandlers(configurator);
    }

    private static void RegisterMediatorErrorHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<GetAgvErrorsRequestHandler>();
        configurator.AddRequestClient<GetAgvErrors>();
        configurator.AddRequestClient<GetActiveAgvErrors>();
        configurator.AddRequestClient<GetActiveErrorsForAgvs>();
        configurator.AddRequestClient<GetErrorHistoryForAgvs>();
    }

    private static void RegisterMediatorActionGroupHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<ActionGroupRequestHandler>();
        configurator.AddRequestClient<GetActionGroup>();
        configurator.AddRequestClient<GetActionGroups>();
        configurator.AddRequestClient<GetActionGroupCountsByState>();
        configurator.AddRequestClient<TriggerActionGroupAbortion>();
        configurator.AddConsumer<ActionCommandHandler>();
        configurator.AddRequestClient<QueueActionGroup>();
    }

    private static void RegisterMediatorAgvCommandHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<RequestAgvDataCommandHandler>();
        configurator.AddRequestClient<RequestAgvData>();

        configurator.AddConsumer<RequestAgvGraphCommandHandler>();
        configurator.AddRequestClient<RequestAgvGraph>();
    }

    private static void RegisterMediatorAgvRequestHandlers(IMediatorRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<GetAgvDataRequestHandler>();
        configurator.AddRequestClient<GetAgvData>();
        configurator.AddRequestClient<GetAgvsData>();

        configurator.AddConsumer<GetCurrentAgvRouteRequestHandler>();
        configurator.AddRequestClient<GetCurrentAgvRoute>();
        configurator.AddRequestClient<GetCurrentAgvRoutes>();

        configurator.AddConsumer<GetGraphRequestHandler>();
        configurator.AddRequestClient<GetAgvGraph>();
        configurator.AddRequestClient<GetAgvGraphs>();

        configurator.AddConsumer<GetAgvPositionsRequestHandler>();
        configurator.AddRequestClient<GetAgvPositions>();
        configurator.AddRequestClient<GetAgvPosition>();

        configurator.AddConsumer<GetAgvsRequestHandler>();
        configurator.AddRequestClient<IsAgvLoadedRequest>();
        configurator.AddRequestClient<GetAgvs>();
        configurator.AddRequestClient<GetAgv>();

        configurator.AddConsumer<CargoRequestHandler>();
        configurator.AddRequestClient<GetCargosOfAgv>();
        configurator.AddRequestClient<GetCargosOfAgvs>();
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

        configurator.AddRequestClient<GetMapGraph>();
        
        configurator.AddConsumer<GetStationsRequestHandler>();
        configurator.AddRequestClient<GetStations>();
    }
}