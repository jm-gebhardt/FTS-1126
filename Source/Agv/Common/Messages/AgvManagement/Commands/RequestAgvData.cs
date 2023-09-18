using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Command to request the data of an AGV.
/// </summary>
[PublicAPI]
public record RequestAgvData : CorrelatedBy<Guid>, IAgvId
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string AgvId { get; }

    /// <summary>
    /// Build message
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="agvId"></param>
    public RequestAgvData(Guid correlationId, string agvId)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
    }
}