using Gebhardt.Acc.Common.Filter.ReadModelParser;

namespace Gebhardt.Acc.Common.Filter.Exceptions;

/// <summary>
/// Represents an exception raised, when the parsing of a <see cref="Filter{TEntity,TReadModel}"/> fails.
/// </summary>
/// <param name="message"></param>
public class FilterParserException(string message) : Exception(message);