using System.Linq.Expressions;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// Abstract base class of filters.
/// </summary>
/// <param name="childNodes">The children attached to this filter.</param>
/// <param name="leaves">The leaves attached to this filter.</param>
public abstract class FilterBase(List<FilterBase> childNodes, List<IFilterLeaf> leaves)
{
    internal const string DomainFilterLayer = "";
    internal const string ReadModelFilterLayer = "::";

    /// <summary>
    /// The children attached to this filter.
    /// </summary>
    public List<FilterBase> ChildNodes => childNodes;

    /// <summary>
    /// The leaves attached to this filter.
    /// </summary>
    public List<IFilterLeaf> Leaves => leaves;

    /// <summary>
    /// Splits a filter into filters that only concern specific sub-properties. The higher the index of the sub-property in the list the more weight it has.
    /// A filter will always be considered to concern the highest weight among its leaves. Guarantees to keep the filter semantic if all the result filters are connected with a logical and.
    /// </summary>
    /// <param name="names">List of the names of sub-properties</param>
    /// <param name="domainAccessors">Domain accessors if they exist.</param>
    /// <returns>
    /// A dictionary pointing from the index of the sub-property in the input list to the biggest filter-subset that concerns it.
    /// </returns>
    public Dictionary<int, FilterBase> SplitFilters(List<string> names, Dictionary<string, LambdaExpression>? domainAccessors = null)
    {
        int currentIndex = GetDeepestSubstringIndex(Leaves, names, domainAccessors);

        if (ChildNodes.Count == 0)
        {
            return new Dictionary<int, FilterBase>([new KeyValuePair<int, FilterBase>(currentIndex, this)]);
        }

        if (StopSplitting())
        {
            return new Dictionary<int, FilterBase>([new KeyValuePair<int, FilterBase>(GetDeepestSubstringIndex(names), this)]);
        }

        Dictionary<int, List<IFilterLeaf>> leafSplits = SplitLeaves(names);
        
        IEnumerable<Dictionary<int, FilterBase>> childSplits = ChildNodes.Select(n => n.SplitFilters(names, domainAccessors));

        Dictionary<int, FilterBase> result = new();
        
        foreach (Dictionary<int, FilterBase> childSplit in childSplits)
        {
            foreach (int index in childSplit.Keys)
            {
                childSplit.TryGetValue(index, out var childFilter);

                if (!result.TryGetValue(index, out var resultFilter))
                {
                    result.Add(index, CreateSameVariant(childFilter));
                    continue;
                }

                if (childFilter != null)
                {
                    resultFilter.ChildNodes.Add(childFilter);
                }
            }
        }

        foreach ((int index, List<IFilterLeaf> leaf) in leafSplits)
        {
            if (!result.TryGetValue(index, out var resultFilter))
            {
                result.Add(index, CreateSameVariant(leaf));
                continue;
            }
            resultFilter.Leaves.AddRange(leaf);
        }

        return result;
    }

    /// <summary>
    /// Creates the same variant of the filter.
    /// </summary>
    protected abstract FilterBase CreateSameVariant(FilterBase? childFilter);

    /// <inheritdoc cref="CreateSameVariant(FilterBase?)"/>
    protected abstract FilterBase CreateSameVariant(List<IFilterLeaf> leaves);

    /// <summary>
    /// Creates an expression tree from this filter.
    /// </summary>
    /// <param name="accessors">Mapping of parameter name -> accessor expression.</param>
    public abstract LambdaExpression ToExpressionTree(Dictionary<string, LambdaExpression> accessors);

    /// <summary>
    /// Defines when the subtree of the filter can not be further split.
    /// </summary>
    protected abstract bool StopSplitting();

    private static int GetDeepestSubstringIndex(IList<IFilterLeaf> leaves, List<string> names, Dictionary<string, LambdaExpression>? domainAccessors = null)
    {
        int min = 0;

        if (!leaves.Select(l => domainAccessors?.ContainsKey(l.Name) is true).All(b => b))
        {
            min = names.FindIndex(s => s == ReadModelFilterLayer);
        }

        if (leaves.Count == 0)
        {
            return 0;
        }

        return Math.Max(leaves.Max(l => names
            .Select((n, i) => l.Name.StartsWith(n) ? i : 0)
            .Max()
        ), min);
    }

    private int GetDeepestSubstringIndex(List<string> names, Dictionary<string, LambdaExpression>? domainAccessors = null)
    {
        int currentIndex = GetDeepestSubstringIndex(Leaves, names, domainAccessors);

        return ChildNodes.Count == 0 ? currentIndex : Math.Max(currentIndex, ChildNodes.Select(n => n.GetDeepestSubstringIndex(names, domainAccessors)).Max());
    }

    private Dictionary<int, List<IFilterLeaf>> SplitLeaves(IList<string> names)
    {
        Dictionary<int, List<IFilterLeaf>> result = new();

        foreach (var leaf in Leaves)
        {
            int leafIndex = names.Select((n, i) => leaf.Name.StartsWith(n) ? i : 0).Max();
            if (!result.TryGetValue(leafIndex, out List<IFilterLeaf>? currentLeaves))
            {
                result.Add(leafIndex, [leaf]);
                continue;
            }

            currentLeaves.Add(leaf);
        }

        return result;
    }

    /// <summary>
    /// String separator for accessing the next level in a property chain.
    /// </summary>
    public const string AccessLevelSeparator = ".";
}