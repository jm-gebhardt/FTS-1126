namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represent the exception raise, when a cargo state does not match the state of the action.
/// </summary>
public class CargoStateMismatchException: Exception
{
    /// <summary>
    /// Represent the exception raise, when a cargo state does not match the state of the action.
    /// </summary>
    /// <param name="actionCorrelationId">The action for which the exception occured.</param>
    /// <param name="hasCargo">True, if the agv has cargo.</param>
    public CargoStateMismatchException(Guid actionCorrelationId, bool hasCargo) : base($"Cargo state {hasCargo} does not match state of action {actionCorrelationId}"){}
}