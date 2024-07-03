using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// The current route of the Agv. It is null when the Agv has completed the last action.
/// </summary>
[PublicAPI]
public record CurrentRouteDto : DatedMessage
{
    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    [UsedImplicitly]
    public Guid CorrelationId { get; init; }

    /// <summary>The remaining nodes.</summary>
    public string[] RemainingNodes { get; init; }
    
    /// <summary>
    /// The remaining edges.
    /// </summary>
    public Guid[] RemainingEdges { get; init; }

    /// <inheritdoc cref="CurrentRouteDto"/>>
    public CurrentRouteDto(Guid correlationId, string[] remainingNodes, Guid[] remainingEdges)
    {
        CorrelationId = correlationId;
        RemainingNodes = remainingNodes;
        RemainingEdges= remainingEdges;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    [PublicAPI]
    private CurrentRouteDto()
    {
        RemainingNodes = null!;
        RemainingEdges = null!;
    }
}