using Gebhardt.Acc.Common.Filter.FilterableParser;

namespace Gebhardt.Acc.Common.Filter.Exceptions;

/// <summary>
/// Represents an exception raised, when the parsing of a <see cref="FilterableBinder{TDomainObject,TReadModel}"/> fails.
/// </summary>
public class FilterParserException(string message) : Exception(message);