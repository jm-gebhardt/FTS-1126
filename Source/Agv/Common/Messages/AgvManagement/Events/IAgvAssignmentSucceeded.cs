using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{

    /// <summary>
    /// An event that provides a assigned AGV for an order 
    /// </summary>
    [PublicAPI]
    public interface IAgvAssignmentSucceeded : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Identifier of the Assigned AGV
        /// </summary>
        string AgvId { get; }
        
        /// <summary>
        /// The AGV correlation id 
        /// </summary>
        Guid AgvCorrelationId { get;  }
    }
}