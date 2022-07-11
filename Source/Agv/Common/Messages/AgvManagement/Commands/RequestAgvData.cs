using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Command to request the data of an AGV.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class RequestAgvData : CorrelatedBy<Guid>
    {
	    /// <summary>
	    /// Agv Correlation ID
	    /// </summary>
	    public Guid CorrelationId { get; }

        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        public string AgvId { get; }


        /// <summary>
        /// Build message
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvId"></param>
        public RequestAgvData(Guid correlationId, string agvId)
        {
	        CorrelationId = correlationId;
	        AgvId = agvId;
        }
    }
}