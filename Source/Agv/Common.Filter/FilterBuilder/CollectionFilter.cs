using System.Linq.Expressions;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// Represents a filter that applies an aggregation to a collection.
/// </summary>
[PublicAPI]
public sealed class CollectionFilter : FilterBase
{
    /// <summary>
    /// Represents a filter that applies an aggregation to a collection.
    /// </summary>
    /// <param name="variant">The aggregation variant.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="leaf">The leaf to apply inside the aggregation.</param>
    public CollectionFilter(CollectionAggregateVariant variant, string collectionName, IFilterLeaf leaf) : base([], [leaf])
    {
        AggregateVariant = variant;
        CollectionName = collectionName;
    }

    /// <summary>
    /// Represents a filter that applies an aggregation to a collection.
    /// </summary>
    /// <param name="variant">The aggregation variant.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="subFilter">The sub filter to apply inside the aggregation if any.</param>
    public CollectionFilter(CollectionAggregateVariant variant, string collectionName, FilterBase? subFilter) : base(subFilter == null ? [] : [subFilter], [])
    {
        AggregateVariant = variant;
        CollectionName = collectionName;
    }

    /// <summary>
    /// The aggregate variant of this collection filter
    /// </summary>
    public CollectionAggregateVariant AggregateVariant { get; }

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string CollectionName { get; }

    /// <inheritdoc />
    protected override FilterBase CreateSameVariant(FilterBase? childFilter)
    {
        return new CollectionFilter(AggregateVariant, CollectionName, childFilter);
    }

    /// <inheritdoc />
    protected override FilterBase CreateSameVariant(List<IFilterLeaf> leaves)
    {
        return new CollectionFilter(AggregateVariant, CollectionName, leaves.First());
    }

    /// <inheritdoc />
    public override LambdaExpression ToExpressionTree(Dictionary<string, LambdaExpression> accessors)
    {
        var childExpression = Leaves.Count == 1 ? Leaves.First().ToExpressionTree(accessors) : ChildNodes.First().ToExpressionTree(accessors);
        if (childExpression == null)
        {
            throw new FilterExecutionException($"Could not convert subtree for method {nameof(ToExpressionTree)} to expression tree");
        }

        if (!accessors.TryGetValue(CollectionName, out var accessor))
        {
            throw new FilterExecutionException($"No accessor for {CollectionName} in {nameof(CollectionFilter)}");
        }

        var inputType = accessor.Parameters.Select(p => p.Type).First();

        var outputType = accessor.ReturnType;

        Type collectionBaseType;

        if (outputType.IsGenericType && outputType.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            collectionBaseType = outputType.GenericTypeArguments.First();
        }
        else
        {
            collectionBaseType = outputType.GetEnumerableContentType() ?? throw new FilterExecutionException($"Accessor expression tree does not return collection for {nameof(CollectionFilter)}");
        }

        return FilterExpressionBuilder.BuildAggregationPredicate(collectionBaseType, inputType, childExpression, accessor, AggregateVariant);
    }

    /// <inheritdoc />
    protected override bool StopSplitting()
    {
        return AggregateVariant is CollectionAggregateVariant.Any;
    }
}