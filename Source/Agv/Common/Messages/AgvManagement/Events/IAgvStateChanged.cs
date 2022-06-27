using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting the current status of an AGV.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IAgvStateChanged : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        string AgvId { get; }

        /// <summary>
        /// Previous state of AGV
        /// </summary>
        AgvState PreviousState { get; }

        /// <summary>
        /// Current state of AGV
        /// </summary>
        AgvState CurrentState { get; }
    }
}