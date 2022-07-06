using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
    /// <summary>
    /// IAgv is a read-only interface to provide current agv data
    /// </summary>
    public interface IAgv : CorrelatedBy<Guid>
    {

        /// <summary>
        /// Agv Serial Number
        /// </summary>
        string AgvId { get; }

        /// <summary>
        /// 
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
    }
}