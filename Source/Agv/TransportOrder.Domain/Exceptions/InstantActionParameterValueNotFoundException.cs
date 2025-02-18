using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception raised when a parameter value was not found for an agv instant action.
/// </summary>
public class InstantActionParameterValueNotFoundException : NotFoundException
{
    /// <inheritdoc cref="InstantActionParameterValueNotFoundException"/>>
    public InstantActionParameterValueNotFoundException(string parameterKey, string actionType) : base($"Required parameter {parameterKey} of action {actionType}"){}
    
    /// <inheritdoc cref="InstantActionParameterValueNotFoundException"/>>
    public InstantActionParameterValueNotFoundException(string actionType) : base($"No parameters defined for action {actionType}"){}
}