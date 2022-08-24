using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Models
{
    /// <summary>
    /// Controller for host order data
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record Agv
    {
        /// <summary>
        /// CorrelationId
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// AGV Serial Number
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// Current State
        /// </summary>
        public string CurrentState { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Agv"/>
        /// </summary>
        public Agv(Guid correlationId, string agvId, string currentState)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            CurrentState = currentState;
        }

        /// <summary>
        /// Creates an <see cref="Agv"/> instance from <see cref="IAgv"/>
        /// </summary>
        public static Agv FromIAgv(IAgv agv)
        {
            return new Agv(agv.CorrelationId, agv.AgvId, agv.CurrentState);
        }
    }
}
