using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Agv.Commands
{
    /// <summary>
    /// Command to request the assignment of an AGV for a specific order.
    /// </summary>
    [PublicAPI]
    public interface IAssignAgv : CorrelatedBy<Guid>
    {
        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        string OrderId { get; }

        /// <summary>
        /// The source station of the order in the format that is known by the agv 
        /// </summary>
        string Source { get; }

        /// <summary>
        /// The destination station of the order in the format that is known by the agv
        /// </summary>
        string Destination { get; }
    }
}