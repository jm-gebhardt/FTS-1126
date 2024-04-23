namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when an invalid property name was provided.
/// </summary>
public class InvalidAgvSettingsPropertyException : Exception
{
    /// <inheritdoc cref="InvalidAgvSettingsPropertyException"/>>
    public InvalidAgvSettingsPropertyException(string propertyName) : base($"Property '{propertyName}' unknown"){}
}