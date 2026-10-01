using Gebhardt.Acc.Common.Domain.Models.NewGraph;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Graph;

/// <summary>
/// Published after a new graph segment has been persisted.
/// </summary>
/// <param name="GraphSegment">The graph segment that was added.</param>
public record GraphSegmentAdded(GraphSegment GraphSegment);