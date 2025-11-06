using MassTransit;

namespace Gebhardt.Acc.Common.Messages.System;

/// <summary>
/// Represents a command to create host orders.
/// </summary>
/// <param name="CorrelationId">A correlation Id.</param>
/// <param name="Source">The source identifier in the definition of the host.</param>
/// <param name="Destination">The destination identifier in the definition of the host.</param>
/// <param name="Count">The number of orders to create.</param>
public record RequestHostOrderGeneration(Guid CorrelationId, string Source, string Destination, int Count) : CorrelatedBy<Guid>;