using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// An event indicating that a new VDA5050 AGV has been added in the repository.
/// </summary>
[PublicAPI]
public record AgvAddedOrUpdated : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <inheritdoc cref="AgvAddedOrUpdated"/>
    /// <param name="correlationId">AGV Correlation Id.</param>
    /// <param name="serialNumber">AGV serial number.</param>
    public AgvAddedOrUpdated(Guid correlationId, string serialNumber)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
    }
}