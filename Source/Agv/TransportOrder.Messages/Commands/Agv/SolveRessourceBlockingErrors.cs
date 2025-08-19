namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a request to solve all resource blocking errors of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="IgnoreErrorState">If the error state should be ignored when clearing the resources. Default false.</param>
public record SolveResourceBlockingErrors(Guid CorrelationId, bool IgnoreErrorState = false);