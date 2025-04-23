namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a request to solve all ressource blocking errors of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SolveRessourceBlockingErrors(Guid CorrelationId);