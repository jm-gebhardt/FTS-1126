using System.Linq.Expressions;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// Leaf of a parsed filter.
/// </summary>
/// <param name="name">Name of the field or property targeted by this leaf.</param>
/// <param name="variant">The type of the comparison.</param>
/// <param name="content">The content used for the filter operation to compare against.</param>
public class FilterLeaf<TContent>(string name, ComparisonVariant variant, TContent content) : IFilterLeaf
{
    /// <summary>
    /// 
    /// </summary>
    public string Name => name;

    /// <summary>
    /// 
    /// </summary>
    private ComparisonVariant Variant => variant;
    
    /// <summary>
    /// Creates the expression tree for this leaf.
    /// </summary>
    /// <param name="accessors">The accessors for the base object.</param>
    /// <returns></returns>
    /// <exception cref="InvalidCastException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public LambdaExpression ToExpressionTree(Dictionary<string, LambdaExpression> accessors)
    {
        if (!accessors.TryGetValue(Name, out var accessor))
        {
            throw new FilterExecutionException($"No accessor for {Name} in {nameof(FilterLeaf<TContent>)}");
        }

        bool noAccessor = Name == string.Empty;

        var inputType = noAccessor ? typeof(TContent) : accessor.Parameters.Select(p => p.Type).First();

        if (Variant == ComparisonVariant.Like)
        {
            return FilterExpressionBuilder.GenerateContainsPredicate(inputType, content, Name, noAccessor ? null : accessor);
        }

        bool isNullable = Nullable.GetUnderlyingType(accessor.ReturnType) != null;

        var predicate = FilterExpressionBuilder.GeneratePredicate(inputType, typeof(TContent), content, Name, Variant, noAccessor ? null : accessor, isNullable);

        return predicate;
    }
}