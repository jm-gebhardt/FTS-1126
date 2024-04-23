using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when an invalid <see cref="AgvState"/> transition was requested.
/// </summary>
public class InvalidStateTransitionException : Exception
{
    /// <summary>
    /// Initializes a new <see cref="InvalidStateTransitionException"/> instance.
    /// </summary>
    /// <param name="description">What nodes were you looking for?</param>
    public InvalidStateTransitionException(string description) : base(description)
    {
    }
}