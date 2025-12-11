using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// Represents a filter that applies an aggregation to a collection.
/// </summary>
[PublicAPI]
public sealed class CollectionFilter : IFilter
{
    /// <summary>
    /// Child of this collection filter.
    /// </summary>
    public IFilter Child;

    /// <summary>
    /// Represents a filter that applies an aggregation to a collection.
    /// </summary>
    /// <param name="variant">The aggregation variant.</param>
    /// <param name="collectionName">The name of the collection.</param>
    /// <param name="child">The sub filter to apply inside the aggregation.</param>
    public CollectionFilter(CollectionAggregateVariant variant, string collectionName, IFilter child)
    {
        AggregateVariant = variant;
        CollectionName = collectionName;
        Child = child;
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
    public Dictionary<int, IFilter> SplitFilter(List<string> names, Dictionary<string, (LambdaExpression, bool)>? domainAccessors = null)
    {
        return StopSplitting() 
            ? new Dictionary<int, IFilter> { { GetDeepestSubstringIndex(names, domainAccessors?.Keys), this } } 
            : Child.SplitFilter(names, domainAccessors).ToDictionary(kv => kv.Key, IFilter (kv) => new CollectionFilter(AggregateVariant, CollectionName, kv.Value));
    }

    /// <inheritdoc />
    public LambdaExpression? ToExpressionTree(Dictionary<string, (LambdaExpression, bool)> accessors)
    {
        try
        {
            if (AggregateVariant is CollectionAggregateVariant.Where)
            {
                return null;
            }

            var childExpression = Child.ToExpressionTree(accessors);

            if (childExpression is null)
            {
                return null;
            }

            if (!accessors.TryGetValue(CollectionName, out var accessorInfo) || accessorInfo is not { Item1: { } accessor })
            {
                throw new FilterExecutionException($"No accessor for {CollectionName} in {nameof(CollectionFilter)}");
            }
            
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
            return FilterExpressionBuilder.CreateAggregationPredicate(collectionBaseType, childExpression, accessor, AggregateVariant);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.GetBaseExceptionKeepingStacktrace();
        }
    }

    /// <inheritdoc />
    public Dictionary<string, IFilter> SeparateCollectionRestrictions(Dictionary<string, IFilter> accumulator)
    {
        if (AggregateVariant is not CollectionAggregateVariant.Where)
        {
            return Child.SeparateCollectionRestrictions(accumulator);
        }
        if (!accumulator.TryGetValue(CollectionName, out var filter))
        {
            accumulator[CollectionName] = Child;
        }
        else if (filter is CombinationFilter { CombinationVariantType: LogicalCombinationVariant.And } combFilter)
        {
            combFilter.Children.Add(Child);
        }
        else
        {
            accumulator[CollectionName] = new CombinationFilter(LogicalCombinationVariant.And, [filter, Child]);
        }
        return accumulator;
    }

    private bool StopSplitting()
    {
        return AggregateVariant is not CollectionAggregateVariant.All;
    }

    /// <inheritdoc />
    public int GetDeepestSubstringIndex(List<string> names, ICollection<string>? earlyBreaks = null)
    {
        if (AggregateVariant is CollectionAggregateVariant.Where)
        {
            return 0;
        }

        var maxSearch = ^0;

        if (earlyBreaks?.Contains(CollectionName) ?? false)
        {
            maxSearch = names.FindIndex(e => e == IFilter.ReadModelFilterLayer) + 1;
        }

        int collectionNameIdx = names[..maxSearch].Select((n, i) => CollectionName.StartsWith(n) ? i : 0).Max();

        return Math.Max(Child.GetDeepestSubstringIndex(names, earlyBreaks), collectionNameIdx);
    }
}