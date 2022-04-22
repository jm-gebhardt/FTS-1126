using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events
{

    /// <summary>
    /// An event that provides a assigned AGV for an order 
    /// </summary>
    [PublicAPI]
    public interface IAgvAssignSucceeded : CorrelatedBy<Guid>
    {
        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        string OrderId { get; }

        /// <summary>
        /// Identifier of the Assigned AGV
        /// </summary>
        string AssignedAgv { get; }
    }
}