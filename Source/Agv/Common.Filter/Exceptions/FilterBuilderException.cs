namespace Gebhardt.Acc.Common.Filter.Exceptions;

/// <summary>
/// Represents an exception raised when the construction of a filter failed.
/// </summary>
public class FilterBuilderException(string message) : Exception(message);