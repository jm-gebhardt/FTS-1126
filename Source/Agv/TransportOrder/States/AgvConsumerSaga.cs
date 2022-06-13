using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events;


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
        /// AGV states.
        /// </summary>
        public enum AgvState
        {
            /// <summary>
            /// New AGV discovered by receiving IAgvStatusReceived while missing informations evaluating the current state of the AGV.
            /// </summary>
            /// <remarks>Hypothetical, receiving <see cref="IAgvStatusReceived"/> will lead to one of the subsequent states.</remarks>
            Discovered,

            /// <summary>
            /// Idle.
            /// </summary>
            Idle,

            /// <summary>
            /// Busy.
            /// </summary>
            Busy,

            /// <summary>
            /// Busy, maintenance requested.
            /// </summary>
            /// <remarks>Reserved, for future use.</remarks>
            [JetBrains.Annotations.UsedImplicitly]
            BusyRequestForMaintenance,

            /// <summary>
            /// Busy, charging requested.
            /// </summary>
            BusyRequestForCharging,

            /// <summary>
            /// Disturbed.
            /// </summary>
            Error,

            /// <summary>
            /// Charging.
            /// </summary>
            Charging,

            /// <summary>
            /// Maintaining.
            /// </summary>
            Maintenance,

            /// <summary>
            /// Disabled.
            /// </summary>
            /// <remarks>Reserved, for future use.</remarks>
            [JetBrains.Annotations.UsedImplicitly]
            Disabled
        }

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
        public string? Agv { get; private set; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<IAgvStatusReceived> c)
        {
            Agv ??= c.Message.Agv;

            if (c.Message.Disturbed)
            {
                CurrentState = AgvState.Error;
            }
            else if (!c.Message.Automatic)
            {
                CurrentState = AgvState.Maintenance;
            }
            else if (c.Message.Charging)
            {
                CurrentState = AgvState.Charging;
            }
            else if (c.Message.Busy && c.Message.BatteryCharge <= IAgvStatusReceived.BadBatteryLevel)
            {
                CurrentState = AgvState.BusyRequestForCharging;
            }
            else if (c.Message.Busy)
            {
                CurrentState = AgvState.Busy;
            }
            else if (!c.Message.Busy)
            {
                CurrentState = AgvState.Idle;
            }
            else
            {
                CurrentState = AgvState.Discovered;
            }

            await Task.CompletedTask;
        }
    }
}