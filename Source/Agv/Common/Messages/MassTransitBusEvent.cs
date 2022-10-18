namespace Gebhardt.StoreWare.Agv.Common.Messages
{
    /// <summary>
    /// Signaling MassTransit bus events
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record MassTransitBusEvent(MassTransitBusEvent.EventType Type)
    {
        /// <summary>
        /// Defines bus events
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public enum EventType
        {
            /// <summary>
            /// Bus has been started successfully
            /// </summary>
            PostStarted
        }
    }
}
