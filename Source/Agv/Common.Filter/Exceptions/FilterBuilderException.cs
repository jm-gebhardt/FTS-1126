namespace Gebhardt.Acc.Common.Filter.Exceptions;

/// <summary>
/// Represents an expression raised when the construction of a filter failed.
/// </summary>
/// <param name="message"></param>
public class FilterBuilderException(string message) : Exception(message);