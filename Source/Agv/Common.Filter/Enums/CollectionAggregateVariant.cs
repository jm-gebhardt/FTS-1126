namespace Gebhardt.Acc.Common.Filter.Enums;

/// <summary>
/// Represents a logical aggregate variant for a collection.
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
    Any,

    /// <summary>
    /// Instead of applying to the entire result set, this variant will reduce the contents of the targeted collection. <br/>
    /// Multiple instances targeting the same collection will be combined with and. <br/>
    /// Nesting this variant has no effect. It will always be treated as a top-level filter on its own. <para/>
    /// </summary>
    Where
}