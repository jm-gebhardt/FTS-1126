using System.Linq.Expressions;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// Represents a filter that combines multiple filters.
/// </summary>
/// <param name="combinationVariantType"></param>
/// <param name="childNodes"></param>
/// <param name="leaves"></param>
[PublicAPI]
public sealed class CombinationFilter(LogicalCombinationVariant combinationVariantType, List<FilterBase> childNodes, List<IFilterLeaf> leaves) : FilterBase(childNodes, leaves)
{
    /// <summary>
    /// The way that all the children of this filter are combined.
    /// </summary>
    public LogicalCombinationVariant CombinationVariantType => combinationVariantType;

    /// <inheritdoc />
    public override LambdaExpression ToExpressionTree(Dictionary<string, LambdaExpression> accessors)
    {
        List<LambdaExpression> childExpressions = ChildNodes.Select(n => n.ToExpressionTree(accessors)).Concat(Leaves.Select(l => l.ToExpressionTree(accessors))).ToList();

        if (childExpressions.Count == 1)
        {
            return childExpressions.First();
        }

        accessors.TryGetValue(DomainFilterLayer, out var lambda);
        var inputType = lambda?.Parameters.FirstOrDefault()?.Type;

        if (inputType == null)
        {
            throw new FilterBuilderException("Could not determine input type in direct filter");
        }

        return childExpressions.Count == 0 ? 
            FilterExpressionBuilder.CreateConstantTruePredicate(inputType) : 
            childExpressions.Aggregate((p1, p2) => FilterExpressionBuilder.ConnectPredicates(p1, p2, CombinationVariantType));
    }

    /// <inheritdoc cref="FilterBase"/>
    protected override bool StopSplitting()
    {
        return CombinationVariantType == LogicalCombinationVariant.Or;
    }

    /// <inheritdoc cref="FilterBase"/>
    protected override FilterBase CreateSameVariant(FilterBase? childFilter)
    {
        return new CombinationFilter(
            CombinationVariantType,
            childFilter == null ? [] : [childFilter],
            []
        );
    }

    /// <inheritdoc cref="FilterBase"/>
    protected override FilterBase CreateSameVariant(List<IFilterLeaf> leaves)
    {
        return new CombinationFilter(CombinationVariantType, [], leaves);
    }
}