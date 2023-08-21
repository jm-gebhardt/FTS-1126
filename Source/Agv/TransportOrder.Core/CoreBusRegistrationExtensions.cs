using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Saga;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core;

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

        busRegistrationConfigurator.AddConsumer<AgvDataConsumer>();
        busRegistrationConfigurator.AddConsumer<ActionConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvPositionReceivedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvBatteryChangedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvGraphReceivedConsumer>();
        busRegistrationConfigurator.AddConsumer<AgvErrorStatusConsumer>();
        busRegistrationConfigurator.AddConsumer<AcceptAgvCommandHandler>();
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
        configurator.AddRequestClient<GetMapImageRequest>();

        configurator.AddConsumer<GetMapListRequestHandler>();
        configurator.AddRequestClient<GetMapListRequest>();

        // agv
        configurator.AddConsumer<RequestAgvDataCommandHandler>();
        configurator.AddRequestClient<RequestAgvDataCommand>();

        configurator.AddConsumer<RequestAgvGraphCommandHandler>();
        configurator.AddRequestClient<RequestAgvGraphCommand>();

        configurator.AddConsumer<GetAgvDataRequestHandler>();
        configurator.AddRequestClient<GetAgvDataRequest>();

        configurator.AddConsumer<GetAgvGraphRequestHandler>();
        configurator.AddRequestClient<GetAgvGraphRequest>();

        configurator.AddConsumer<GetAgvPositionsRequestHandler>();
        configurator.AddRequestClient<GetAgvPositionsRequest>();

        configurator.AddConsumer<GetAgvsDtoRequestHandler>();
        configurator.AddRequestClient<GetAgvsDtoRequest>();

        configurator.AddConsumer<GetAgvDtoRequestHandler>();
        configurator.AddRequestClient<GetAgvDtoRequest>();

        configurator.AddConsumer<LockAndUnlockAgvCommandHandler>();
        configurator.AddRequestClient<LockAgvCommand>();
        configurator.AddRequestClient<UnlockAgvCommand>();

        configurator.AddConsumer<GetAgvActionGroupsRequestHandler>();
        configurator.AddRequestClient<GetAgvActionGroupsRequest>();

        configurator.AddConsumer<GetAgvActionGroupCountsByStateRequestHandler>();
        configurator.AddRequestClient<GetAgvActionGroupCountsByStateRequest>();

        configurator.AddConsumer<MoveAgvCommandHandler>();
        configurator.AddRequestClient<MoveAgv>();

        // actions
        configurator.AddConsumer<ActionConsumer>();
        configurator.AddRequestClient<QueueActionGroup>();

        // error
        configurator.AddConsumer<GetActiveAgvErrorsRequestHandler>();
        configurator.AddRequestClient<GetActiveAgvErrorsRequest>();

        configurator.AddConsumer<GetAgvErrorsRequestHandler>();
        configurator.AddRequestClient<GetAgvErrorsRequest>();
    }
}