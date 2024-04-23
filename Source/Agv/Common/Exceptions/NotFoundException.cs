namespace Gebhardt.Acc.Common.Exceptions;

/// <summary>
/// Represents errors that occur when something is not found, e.g. an object in the e.g. repository
/// </summary>
public abstract class NotFoundException : Exception
{
    /// <summary>
    /// <inheritdoc cref="NotFoundException"/>
    /// </summary>
    /// <param name="paramName"><example>"Agv"</example></param>
    protected NotFoundException(string paramName) : base($"{paramName} not found")
    {
    }

    /// <summary>
    /// <inheritdoc cref="NotFoundException"/>
    /// </summary>
    /// <param name="paramName"><example>"Agv"</example></param>
    /// <param name="paramIdentifierName"><example>"CorrelationId"</example></param>
    /// <param name="paramIdentifierValue"><example>7d0d4300-1f4b-44df-af4b-297fa9790d1d</example></param>
    protected NotFoundException(string paramName, string paramIdentifierName, object? paramIdentifierValue)
        : base($"{paramName} not found using the {paramIdentifierName}:{paramIdentifierValue}.")
    {
    }
}