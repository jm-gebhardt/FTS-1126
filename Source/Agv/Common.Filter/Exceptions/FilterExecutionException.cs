namespace Gebhardt.Acc.Common.Filter.Exceptions;

/// <summary>
/// Represents an exception raised when the execution of a filter fails.
/// </summary>
/// <param name="message"></param>
public class FilterExecutionException(string message) : Exception(message);