using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when an invalid <see cref="AgvState"/> was constructed.
/// </summary>
[UsedImplicitly]
public class InvalidAgvStateException : Exception
{
    /// <summary>
    /// Initializes a new <see cref="InvalidAgvStateException"/> instance.
    /// </summary>
    /// <param name="description">What nodes were you looking for?</param>
    public InvalidAgvStateException(string description) : base(description)
    {
    }
}