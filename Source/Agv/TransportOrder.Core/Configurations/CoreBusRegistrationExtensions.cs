using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using MassTransit;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;

/// <summary>
/// IBusRegistrationExtensions extensions
/// </summary>
public static class CoreBusRegistrationExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="busRegistrationConfigurator"></param>
    /// <returns></returns>
    public static IBusRegistrationConfigurator AddCoreConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddSagaStateMachine<AgvStateMachine, AgvSaga>()
            .AddPersistence<AgvSaga, SagaPersistenceDbContext>();
        busRegistrationConfigurator.AddDelayedMessageScheduler();

        busRegistrationConfigurator.AddConsumer<AgvDataEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvPositionReceivedEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvGraphReceivedEventHandler>();
        busRegistrationConfigurator.AddConsumer<AgvErrorStatusEventHandler>();
        busRegistrationConfigurator.AddConsumer<AcceptAgvCommandHandler>();
        busRegistrationConfigurator.AddConsumer<ActionCommandHandler>();
        busRegistrationConfigurator.AddConsumer<ActionEventHandler>();
        busRegistrationConfigurator.AddConsumer<ActionCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(16);
            c.Message<CreateParkingActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.Agv.CorrelationId));
            c.Message<CreateChargingActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.Agv.CorrelationId));
            c.Message<CreateMaintenanceActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.Agv.CorrelationId));
            c.Message<TryStartNextActionInActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.Agv.CorrelationId));
            c.Message<TryStartNewActionGroup>(x =>
                x.UsePartitioner(partitioner, p => p.Message.Agv.CorrelationId));
            c.Message<TryStartManualAction>(x =>
                x.UsePartitioner(partitioner, p => p.Message.Agv.CorrelationId));
        });

        busRegistrationConfigurator.AddConsumer<MapCommandHandler>(cfg =>
        {
          cfg.Message<SaveMap>(x => x.UsePartitioner(16, p => p.Message.Map.CorrelationId));
        });
        busRegistrationConfigurator.AddConsumer<AgvCommandHandler>();
        busRegistrationConfigurator.AddConsumer<AgvEventHandler>();
        
        busRegistrationConfigurator.AddRequestClient<AcceptAgvCommand>();
        

        return busRegistrationConfigurator;
    }

    /// <summary>
    /// Adds the MassTransit Mediator
    /// MassTransit Mediator runs in-process and in-memory, no transport is required.
    /// </summary>
    /// <param name="configurator"></param>
    public static void AddMediator(this IMediatorRegistrationConfigurator configurator)
    {
        // maps
        configurator.AddConsumer<GetMapImageRequestHandler>();
        configurator.AddRequestClient<GetMapImage>();

        configurator.AddConsumer<GetMapListRequestHandler>();
        configurator.AddRequestClient<GetMapList>();

        // agv
        configurator.AddConsumer<RequestAgvDataCommandHandler>();
        configurator.AddRequestClient<RequestAgvData>();

        configurator.AddConsumer<RequestAgvGraphCommandHandler>();
        configurator.AddRequestClient<RequestAgvGraph>();

        configurator.AddConsumer<GetAgvDataRequestHandler>();
        configurator.AddRequestClient<GetAgvDataRequest>();

        configurator.AddConsumer<GetAgvGraphRequestHandler>();
        configurator.AddRequestClient<GetAgvGraph>();

        configurator.AddConsumer<GetAgvPositionsRequestHandler>();
        configurator.AddRequestClient<GetAgvPositions>();

        configurator.AddConsumer<GetAgvsRequestHandler>();
        configurator.AddRequestClient<GetAgvs>();
        configurator.AddRequestClient<GetAgv>();

        configurator.AddConsumer<LockAndUnlockAgvCommandHandler>();
        configurator.AddRequestClient<LockAgv>();
        configurator.AddRequestClient<UnlockAgv>();

        configurator.AddConsumer<SetAgvForceIdleCommandHandler>();
        configurator.AddRequestClient<SetAgvForceIdle>();

        configurator.AddConsumer<MoveAgvCommandHandler>();
        configurator.AddRequestClient<MoveAgv>();

        // actions
        configurator.AddConsumer<AgvEventHandler>();
        configurator.AddRequestClient<QueueActionGroup>();

        configurator.AddConsumer<ActionGroupRequestHandler>();
        configurator.AddRequestClient<GetActionGroup>();
        configurator.AddRequestClient<GetActionGroups>();
        configurator.AddRequestClient<GetActionGroupCountsByState>();


        // error
        configurator.AddRequestClient<GetActiveAgvErrorsRequest>();

        configurator.AddConsumer<GetAgvErrorsRequestHandler>();
        configurator.AddRequestClient<GetAgvErrorsRequest>();
    }
}