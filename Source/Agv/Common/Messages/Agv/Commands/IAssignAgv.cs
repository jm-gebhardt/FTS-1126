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
        /// Order Details
        /// </summary>
        Order Order { get; }
    }
}