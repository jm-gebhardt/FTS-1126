namespace Gebhardt.Acc.Common.Domain.Exceptions;

/// <summary>
/// Exception raised when a string property value exceeds the maximum length allowed.
/// </summary>
public class StringLengthExceededException : ValidationException
{
    /// <inheritdoc cref="StringLengthExceededException"/>
    /// <param name="actualLength">The actual length of the string value.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="propertyName">The name of the property that exceeded the limit.</param>
    /// <param name="contextName">The name of the context (e.g., class or entity) where the property is located.</param>
    public StringLengthExceededException(int actualLength, int maxLength, string propertyName, string? contextName = null)
        : base($"Property '{propertyName}' {(!string.IsNullOrEmpty(contextName) ? $"on '{contextName}'" : "")} exceeds the maximum allowed length of {maxLength} characters (actual: {actualLength}).")
    {
    }
}