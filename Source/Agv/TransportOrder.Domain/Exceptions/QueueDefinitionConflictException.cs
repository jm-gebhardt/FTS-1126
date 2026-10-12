namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>A queue name already exists with different nodes or positions.</summary>
public class QueueDefinitionConflictException(string name)
    : InvalidOperationException($"Queue '{name}' already exists with a different node/position definition.");
