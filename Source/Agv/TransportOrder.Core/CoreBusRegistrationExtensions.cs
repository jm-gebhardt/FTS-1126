using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Saga;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core
{
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
        public static void AddCoreConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
        {
            busRegistrationConfigurator.AddSagaStateMachine<AgvStateMachine, AgvSaga>()
                .AddPersistence<AgvSaga, SagaPersistenceDbContext>();

            // consumer
            // ########################################
            busRegistrationConfigurator.AddConsumer<AgvDataConsumer>();
            busRegistrationConfigurator.AddConsumer<ActionConsumer>();
            busRegistrationConfigurator.AddConsumer<AgvPositionReceivedConsumer>();
            busRegistrationConfigurator.AddConsumer<AgvBatteryChangedConsumer>();

            // maps
            busRegistrationConfigurator.AddConsumer<GetMapImageRequestHandler>();
            busRegistrationConfigurator.AddConsumer<GetMapListRequestHandler>();

            // agv
            busRegistrationConfigurator.AddConsumer<RequestAgvDataCommandHandler>();
            busRegistrationConfigurator.AddConsumer<RequestAgvGraphCommandHandler>();
            busRegistrationConfigurator.AddConsumer<GetAgvDataRequestHandler>();
            busRegistrationConfigurator.AddConsumer<GetAgvGraphRequestHandler>();
            busRegistrationConfigurator.AddConsumer<GetAgvPositionsRequestHandler>();
            busRegistrationConfigurator.AddConsumer<GetAgvsRequestHandler>();
            busRegistrationConfigurator.AddConsumer<LockAgvCommandHandler>();
            busRegistrationConfigurator.AddConsumer<UnlockAgvCommandHandler>();
            busRegistrationConfigurator.AddConsumer<GetAgvActionGroupsRequestHandler>();
            busRegistrationConfigurator.AddConsumer<GetAgvActionGroupCountsByStateRequestHandler>();

            // error
            busRegistrationConfigurator.AddConsumer<GetActiveAgvErrorsRequestHandler>();
            busRegistrationConfigurator.AddConsumer<GetAgvErrorsRequestHandler>();
            busRegistrationConfigurator.AddConsumer<AgvErrorStatusConsumer>();

            // request clients
            // ########################################
            // maps
            busRegistrationConfigurator.AddRequestClient<GetMapImageRequest>();
            busRegistrationConfigurator.AddRequestClient<GetMapListRequest>();

            // agv
            busRegistrationConfigurator.AddRequestClient<RequestAgvDataCommandHandler>();
            busRegistrationConfigurator.AddRequestClient<RequestAgvGraphCommandHandler>();
            busRegistrationConfigurator.AddRequestClient<GetAgvDataRequestHandler>();
            busRegistrationConfigurator.AddRequestClient<GetAgvGraphRequestHandler>();
            busRegistrationConfigurator.AddRequestClient<GetAgvPositionsRequestHandler>();
            busRegistrationConfigurator.AddRequestClient<GetAgvsRequestHandler>();
            busRegistrationConfigurator.AddRequestClient<LockAgvCommandHandler>();
            busRegistrationConfigurator.AddRequestClient<UnlockAgvCommandHandler>();
            busRegistrationConfigurator.AddRequestClient<GetAgvActionGroupsRequestHandler>();
            busRegistrationConfigurator.AddRequestClient<GetAgvActionGroupCountsByStateRequestHandler>();

            // error
            busRegistrationConfigurator.AddRequestClient<GetActiveAgvErrorsRequestHandler>();
            busRegistrationConfigurator.AddRequestClient<GetAgvErrorsRequestHandler>();

            busRegistrationConfigurator.AddDelayedMessageScheduler();
        }
    }
}
