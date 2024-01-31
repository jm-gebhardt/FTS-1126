using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the current position of an AGV.
/// </summary>
[PublicAPI]
public record AgvPositionReceived : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// Current AGV position
    /// </summary>
    public AgvPosition AgvPosition { get; }

    /// <inheritdoc cref="AgvPositionReceived"/>
    public AgvPositionReceived(Guid correlationId, string serialNumber, AgvPosition agvPosition)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        AgvPosition = agvPosition;
    }
}