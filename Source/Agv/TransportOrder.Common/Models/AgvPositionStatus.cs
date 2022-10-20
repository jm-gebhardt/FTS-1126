using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Common.Models
{
    /// <summary>
    /// Model for the status of the AGV position
    /// </summary>
    /// <param name="CorrelationId">The correlationId of the new AGV position</param>
    /// <param name="AgvId">Identifier of the AGV</param>
    /// <param name="AgvPosition">AGV position</param>
    [JetBrains.Annotations.PublicAPI]
    public record AgvPositionStatus(Guid CorrelationId, string AgvId, AgvPosition AgvPosition);
}
