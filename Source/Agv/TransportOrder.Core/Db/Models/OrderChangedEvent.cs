using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models
{
    /// <summary>
    /// Class representing the OrderChangedEvent in database
    /// </summary>
    public class OrderChangedEvent : ISaga
    {
        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// CorrelationId related to OrderSaga entry
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public OrderSaga OrderSaga { get; set; }

        /// <summary>
        /// Name of the event which caused the change in the OrderSaga
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public string EventName { get; }

        /// <summary>
        /// Describing the OrderSaga's state when a Hook is raised 
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public string State { get; }

        /// <summary>
        /// Indicating whether it is a PreHook or a PostHook
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public string Hook { get; }

        /// <summary>
        /// Timestamp of the entry's last manipulation in the EfOrderAuditRegistry 
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="correlationId">Unique id in DB</param>
        /// <param name="orderSaga">Referring to entry in OrderSaga</param>
        /// <param name="eventName"></param>
        /// <param name="state"></param>
        /// <param name="hook"></param>
        /// <param name="timeStamp"></param>
        public OrderChangedEvent(Guid correlationId, OrderSaga orderSaga, string eventName, string state, string hook, DateTime timeStamp)
        {
            CorrelationId = correlationId;
            OrderSaga = orderSaga;
            EventName = eventName;
            State = state;
            Hook = hook;
            TimeStamp = timeStamp;
        }
    }

    /// <summary>
    /// Distinguishing Hooks
    /// </summary>
    public enum Hook
    {
        /// <summary/>
        PreHook,

        /// <summary/>
        PostHook,

        /// <summary/>
        ExecuteFault
    }
}