using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Common.Exceptions;

/// <summary>
/// The specific load was not found on the AGV.
/// </summary>
public class LoadNotFoundException : ValidationException
{
    /// <inheritdoc cref="LoadNotFoundException"/>
    public LoadNotFoundException(string loadId)
        : base($"Load '{loadId}' not found.")
    {
    }
}