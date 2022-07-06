using MassTransit;


namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting the current status of an AGV.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IAgvStatusReceived : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        string AgvId { get; }
        
        /// <summary>
        /// AgvStatus 
        /// </summary>
        public AgvStatus AgvStatus { get; }
    }
}