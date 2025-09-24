namespace Gebhardt.Acc.Common.Filter.Exceptions;

/// <summary>
/// Represents an exception raised when the execution of a filter fails.
/// </summary>
public class FilterExecutionException(string message) : Exception(message);