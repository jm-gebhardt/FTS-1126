namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>A queue references graph nodes that have not been persisted.</summary>
public class QueueNodesNotReadyException(string name, IEnumerable<Guid> missingNodeIds)
    : InvalidOperationException($"Queue '{name}' nodes are not ready. Missing graph node IDs: {string.Join(", ", missingNodeIds)}");