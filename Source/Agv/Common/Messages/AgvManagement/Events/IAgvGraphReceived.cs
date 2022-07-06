using MassTransit;


namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting the graph of an AGV.
    /// </summary><remarks>
    /// This data structure is optimized for
    /// information purpose by keeping additional information like coordinates and for
    /// routing algorithms by having information like metric.
    /// </remarks>
    [JetBrains.Annotations.PublicAPI]
    public interface IAgvGraphReceived : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        string AgvId { get; }
        /// <summary>
        /// Agv Graph
        /// </summary>
        public AgvGraph Graph { get; } 
    }

}
