namespace Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;

/// <summary>
/// Defines a sub property that is joined to the queryable at filter execution. Must be instanceof <see cref="IFilterable{TEntity,TReadModel}"/>.
/// The key properties must match in type.
/// </summary>
/// <param name="ownKeyProperty">The key property on this type.</param>
/// <param name="otherKeyProperty">The key property on the target type.</param>
/// <param name="isOuterJoin">If this is a LEFT OUTER JOIN.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JoinProperty(string ownKeyProperty, string otherKeyProperty, bool isOuterJoin = false) : InternalSubProperty
{
    /// <summary>
    /// The key property on this type.
    /// </summary>
    public string OwnKeyProperty => ownKeyProperty;

    /// <summary>
    /// The key property on the target type.
    /// </summary>
    public string OtherKeyProperty => otherKeyProperty;

    /// <summary>
    /// If this is a LEFT OUTER JOIN.
    /// </summary>
    public bool IsOuterJoin => isOuterJoin;
}