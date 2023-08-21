using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Answers whether the Agv can go to busy state
/// </summary>
[JetBrains.Annotations.PublicAPI]
public sealed class CanAgvTransitionToBusyResponse : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Can the Agv go to busy state?
    /// </summary>
    public bool Availability { get; }

    /// <summary>
    /// SetAgvBusySucceeded Constructor
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="availability">Can the Agv go to busy state?</param>
    public CanAgvTransitionToBusyResponse(Guid correlationId, bool availability)
    {
        CorrelationId = correlationId;
        Availability = availability;
    }
}