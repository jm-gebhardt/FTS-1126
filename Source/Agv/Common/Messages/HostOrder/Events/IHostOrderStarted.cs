using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
    /// <summary>
    /// An event that reports the successful start of a transport order by the specified AGV
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IHostOrderStarted: CorrelatedBy<Guid>
    {
        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        string OrderId { get; }

        /// <summary>
        /// The assigned AGV
        /// </summary>
        string AssignedAgv { get; }
    }
}