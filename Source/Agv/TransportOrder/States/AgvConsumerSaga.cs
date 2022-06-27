using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.TransportOrder.States
{
    /// <summary>
    /// AGV saga representing the current state of an AGV.
    /// <para>
    /// This is built as a consumer saga (<see href="https://masstransit-project.com/usage/sagas/consumer-saga.html" />).
    /// </para></summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvConsumerSaga : ISaga, InitiatedByOrOrchestrates<IAgvStatusReceived>
    {
        /// <summary>
        /// Default constructor required to connect.
        /// </summary>
        public AgvConsumerSaga(Guid correlationId)
        {
            if (correlationId == Guid.Empty)
            {
                throw new InvalidOperationException($"{this.GetType()} requires correlation ID.");
            }
            CorrelationId = correlationId;
        }

        /// <summary>
        /// Current state.
        /// </summary>
        public AgvState CurrentState { get; private set; } = AgvState.Discovered;

        /// <summary>
        /// CorrelationId.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        public string? AgvId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<IAgvStatusReceived> context)
        {
            AgvId ??= context.Message.AgvId;
            var previousState = CurrentState;

            if (context.Message.Disturbed)
            {
                CurrentState = AgvState.Error;
            }
            else if (!context.Message.Automatic)
            {
                CurrentState = AgvState.Maintenance;
            }
            else if (context.Message.Charging)
            {
                CurrentState = AgvState.Charging;
            }
            else if (context.Message.Busy && context.Message.BatteryCharge <= IAgvStatusReceived.BadBatteryLevel)
            {
                CurrentState = AgvState.BusyRequestForCharging;
            }
            else if (context.Message.Busy)
            {
                CurrentState = AgvState.Busy;
            }
            else if (!context.Message.Busy)
            {
                CurrentState = AgvState.Idle;
            }
            else
            {
                CurrentState = AgvState.Discovered;
            }

            if (CurrentState == AgvState.Discovered || CurrentState != previousState) // New AGV or new state
            {
                await context.Publish<IAgvStateChanged>(new 
                { 
                    context.Message.CorrelationId,
                    context.Message.AgvId,
                    PreviousState = previousState,
                    CurrentState
                });
            }

            await Task.CompletedTask;
        }
    }
}