using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the current status of an AGV.
/// </summary>
[PublicAPI]
public record AgvStatusReceived : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// AgvStatus 
    /// </summary>
    public AgvStatus AgvStatus { get; }

    /// <inheritdoc cref="AgvStatusReceived"/>
    public AgvStatusReceived(Guid correlationId, string serialNumber, AgvStatus agvStatus)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        AgvStatus = agvStatus;
    }
}