using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the current position of an AGV.
/// </summary>
[PublicAPI]
public record AgvPositionReceived : CorrelatedBy<Guid>, IAgvId
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string AgvId { get; }

    /// <summary>
    /// Current AGV position
    /// </summary>
    public AgvPosition AgvPosition { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AgvPositionReceived"/>
    /// </summary>
    public AgvPositionReceived(Guid correlationId, string agvId, AgvPosition agvPosition)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        AgvPosition = agvPosition;
    }
}