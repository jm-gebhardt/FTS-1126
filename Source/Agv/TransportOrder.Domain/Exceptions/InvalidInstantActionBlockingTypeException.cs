using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception raised when a given blocking type is invalid for this instant action.
/// </summary>
public class InvalidInstantActionBlockingTypeException : Exception
{
    /// <inheritdoc cref="InvalidInstantActionBlockingTypeException"/>>
    public InvalidInstantActionBlockingTypeException(ActionBlockingType? blockingType, string actionType): base($"Block type {blockingType} not valid for action {actionType}"){}
}