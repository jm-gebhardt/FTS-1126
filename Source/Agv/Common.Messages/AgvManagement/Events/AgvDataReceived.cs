using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the retrieval of AGV data.
/// </summary>
[PublicAPI]
public record AgvDataReceived : CorrelatedBy<Guid>, IAgv
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// Agv Graph
    /// </summary>
    public AgvData Data { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId">Unique AGV Correlation ID</param>
    /// <param name="serialNumber">The serial number of the agv.</param>
    /// <param name="data">AGV data</param>
    public AgvDataReceived(Guid correlationId, string serialNumber, AgvData data)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Data = data;
    }
}