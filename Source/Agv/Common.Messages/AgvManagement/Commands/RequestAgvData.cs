using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Command to request the data of an AGV.
/// </summary>
[PublicAPI]
public record RequestAgvData : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// Build message
    /// </summary>
    /// <param name="correlationId">The correlation Id of the Agv.</param>
    /// <param name="serialNumber">The serial number of the agv.</param>
    public RequestAgvData(Guid correlationId, string serialNumber)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
    }
}