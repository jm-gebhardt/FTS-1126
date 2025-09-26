using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.FilterBuilder;

namespace Gebhardt.Acc.Common.Filter.Exceptions;

/// <summary>
/// Represents an exception that occurred while parsing a <see cref="FilterDto"/> into an <see cref="IFilter"/>
/// </summary>
/// <param name="message"></param>
public class FilterDtoParserException(string message) : Exception(message);