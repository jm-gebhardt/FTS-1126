using MassTransit;

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
        string AgvId { get; }

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
    }
}