using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.Api.Dtos;

/// <summary>
/// Base of a filter data transfer object. Should not be instantiated. Use one of the concrete implementations which display a reference to this model in their &quot;allOf&quot; inheritance: <br/>
/// <ul>
///     <li><see cref="CombinationFilterDto"/></li>
///     <li><see cref="CollectionFilterDto"/></li>
///     <li><see cref="ValueComparisonFilterDto"/></li>
/// </ul>
/// </summary>
[PublicAPI]
public abstract record FilterBaseDto;
