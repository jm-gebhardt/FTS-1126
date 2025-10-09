namespace Gebhardt.Acc.Common.Filter.Enums;

/// <summary>
/// Represents an aggregate variant for a collection.
/// </summary>
public enum CollectionAggregateVariant
{
    /// <summary>
    /// All elements of a collection need to fulfill a predicate.
    /// </summary>
    All,

    /// <summary>
    /// Any element of a collection needs to fulfill a predicate.
    /// </summary>
    Any
}