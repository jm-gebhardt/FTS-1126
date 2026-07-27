using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Common.Exceptions;

/// <summary>
/// The load position is already occupied by another load.
/// </summary>
public class LoadPositionOccupiedException : ValidationException
{
    /// <inheritdoc cref="LoadPositionOccupiedException"/>
    public LoadPositionOccupiedException(string? loadPosition)
        : base($"Load position '{loadPosition}' is already occupied.")
    {
    }
}