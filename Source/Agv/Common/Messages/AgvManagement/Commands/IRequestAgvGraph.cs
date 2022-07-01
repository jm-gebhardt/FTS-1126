using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Command to request the assignment of an AGV for a specific order.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IRequestAgvGraph : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        string AgvId { get; }
    }
}