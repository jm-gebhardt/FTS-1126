using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Agv Status Received was not found
/// </summary>
[PublicAPI]
public class AgvStatusReceivedNotFoundException : NotFoundException
{
    /// <summary>
    /// Agv Status Received not found for Agv with agv correlationId
    /// </summary>
    /// <param name="agvCorrelationId">The Correlation id of Agv</param>
    public AgvStatusReceivedNotFoundException(Guid agvCorrelationId)
        : base(nameof(AgvStatusReceived), nameof(agvCorrelationId), agvCorrelationId)
    {
    }
}