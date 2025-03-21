using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to request the zones of an AGV.
/// </summary>
[PublicAPI]
public record RequestAgvZones :  CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <inheritdoc cref="RequestAgvZones" />
    public RequestAgvZones( Guid correlationId, string serialNumber)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
    }
}