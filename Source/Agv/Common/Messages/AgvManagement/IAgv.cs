using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
    /// <summary>
    /// IAgv is a read-only interface to provide current agv data
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IAgv : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Agv Serial Number
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public string AgvId { get; }

        /// <summary>
        /// The current state of the AGV
        /// </summary>
        public string CurrentState { get; }

        /// <summary>
        /// AGV Status
        /// </summary>
        public AgvStatus? AgvStatus { get; }

        /// <summary>
        /// AGV Graph
        /// </summary>
        public AgvGraph? AgvGraph { get; }

        /// <summary>
        /// AGV Data
        /// </summary>
        public AgvData? AgvData { get; }

        /// <summary>
        /// Returns true when there is a life sign of the AGV in regular time intervals,
        /// returns false when there is no life sign for a certain time.
        /// </summary>
        public bool Alive { get; }

        /// <summary>
        /// Returns true when the AGV is assignable for any transport order.
        /// </summary>
        public bool Assignable { get; }

    }
}
