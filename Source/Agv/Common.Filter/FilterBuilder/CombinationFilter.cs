using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// Represents a filter that combines multiple filters.
/// </summary>
/// <param name="combinationVariantType"></param>
/// <param name="children"></param>
[PublicAPI]
public sealed class CombinationFilter(LogicalCombinationVariant combinationVariantType, List<IFilter> children) : IFilter
{
    internal List<IFilter> Children => children;

    /// <summary>
    /// The way that all the children of this filter are combined.
    /// </summary>
    public LogicalCombinationVariant CombinationVariantType => combinationVariantType;

    /// <inheritdoc />
    public Dictionary<int, IFilter> SplitFilter(List<string> names, Dictionary<string, (LambdaExpression, bool)>? domainAccessors = null)
    {
        if (StopSplitting())
        {
            return new Dictionary<int, IFilter> { { GetDeepestSubstringIndex(names, domainAccessors?.Keys), this } };
        }

        IEnumerable<Dictionary<int, IFilter>> intermediate = Children.Select(c => c.SplitFilter(names, domainAccessors));

        Dictionary<int, CombinationFilter> result = new();

        foreach (Dictionary<int, IFilter> childSplit in intermediate)
        {
            foreach ((int index, var child) in childSplit)
            {
                if (result.TryGetValue(index, out var existingSplit))
                {
                    existingSplit.Children.Add(child);
                    continue;
                }
                result.Add(index, new CombinationFilter(CombinationVariantType, [child]));
            }
        }

        return result.ToDictionary(kv => kv.Key, IFilter (kv) => kv.Value);
    }

    /// <inheritdoc />
    public LambdaExpression ToExpressionTree(Dictionary<string, (LambdaExpression, bool)> accessors)
    {
        try 
        {
            List<LambdaExpression> childExpressions = Children.Select(n => n.ToExpressionTree(accessors)).ToList();

            if (childExpressions.Count == 1)
            {
                return childExpressions.First();
            }

            accessors.TryGetValue(IFilter.DomainFilterLayer, out var lambda);

            var inputType = lambda.Item1?.Parameters.FirstOrDefault()?.Type;

            if (inputType == null)
            {
                throw new FilterBuilderException("Could not determine input type in direct filter");
            }

            return childExpressions.Count == 0 ?
                FilterExpressionBuilder.CreateConstantTruePredicate(inputType) :
                childExpressions.Aggregate((p1, p2) => FilterExpressionBuilder.ConnectPredicates(p1, p2, CombinationVariantType));
        }
        catch (TargetInvocationException ex)
        {
            throw ex.GetBaseException();
        }
    }

    /// <inheritdoc cref="IFilter"/>
    private bool StopSplitting()
    {
        return CombinationVariantType == LogicalCombinationVariant.Or;
    }

    /// <inheritdoc />
    public int GetDeepestSubstringIndex(List<string> names, ICollection<string>? domainAccessorKeys = null)
    {
        return Children.Select(c => c.GetDeepestSubstringIndex(names, domainAccessorKeys)).Max();
    }
}