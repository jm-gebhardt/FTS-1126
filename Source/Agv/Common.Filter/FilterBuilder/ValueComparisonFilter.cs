using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// Represents a value comparison of the specified value to a property.
/// </summary>
/// <param name="name">Name of the field or property targeted by this leaf.</param>
/// <param name="variant">The type of the comparison.</param>
/// <param name="content">The content used for the filter operation to compare against.</param>
public sealed class ValueComparisonFilter<TContent>(string name, ComparisonVariant variant, TContent content) : IValueComparisonFilter
{
    internal string Name => name;

    private ComparisonVariant Variant => variant;

    /// <inheritdoc />
    public Dictionary<int, IFilter> SplitFilter(List<string> names, Dictionary<string, (LambdaExpression, bool)>? domainAccessors = null)
    {
        return new Dictionary<int, IFilter> { { GetDeepestSubstringIndex(names, domainAccessors?.Keys), this } };
    }

    /// <inheritdoc cref="IFilter.ToExpressionTree"/>
    public LambdaExpression ToExpressionTree(Dictionary<string, (LambdaExpression, bool)> accessors)
    {
        try
        {
            if (!accessors.TryGetValue(Name, out var accessorInfo) || accessorInfo is not { Item1: { } accessor })
            {
                throw new FilterExecutionException($"No accessor for {Name} in {nameof(ValueComparisonFilter<>)}");
            }

            bool noAccessor = Name == string.Empty;

            var inputType = noAccessor ? typeof(TContent) : accessor.Parameters.Select(p => p.Type).First();

            if (Variant == ComparisonVariant.Like)
            {
                return FilterExpressionBuilder.GenerateContainsPredicate(inputType, content, Name, noAccessor ? null : accessor);
            }

            bool isNullable = Nullable.GetUnderlyingType(accessor.ReturnType) != null && Nullable.GetUnderlyingType(typeof(TContent)) == null;

            var predicate = FilterExpressionBuilder.GenerateNumericalPredicate(inputType, typeof(TContent), content, Name, Variant, noAccessor ? null : accessor, isNullable);

            return predicate;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.GetBaseExceptionKeepingStacktrace();
        }
    }

    /// <inheritdoc />
    public int GetDeepestSubstringIndex(List<string> names, ICollection<string>? earlyBreaks = null)
    {
        var maxSearch = ^0;

        if (earlyBreaks?.Contains(Name) ?? false)
        {
            maxSearch = names.FindIndex(e => e == IFilter.ReadModelFilterLayer) + 1;
        }

        return names[..maxSearch].Select((n, i) => Name.StartsWith(n) ? i : 0).Max();
    }
}

/// <summary>
/// Non generic marker interface for <see cref="ValueComparisonFilter{TContent}"/>.
/// </summary>
internal interface IValueComparisonFilter : IFilter;