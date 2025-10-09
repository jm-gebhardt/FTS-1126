namespace Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;

/// <summary>
/// Defines a sub property that will be included with the queryable at filter execution. Must be instanceof <see cref="IFilterable{TEntity,TReadModel}"/> or a collection thereof.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NavigationProperty : InternalSubProperty;