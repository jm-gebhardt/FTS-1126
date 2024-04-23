using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Represents the route of an Agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
/// <param name="Nodes">List of node ids that define the route.</param>
[UsedImplicitly]
public record Route(Guid CorrelationId, string[] Nodes);