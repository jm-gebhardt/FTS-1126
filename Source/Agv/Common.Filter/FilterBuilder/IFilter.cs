using System.Linq.Expressions;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// A first order logic object, bound to a specific DomainObject/ReadModel combination.
/// </summary>
public interface IFilter
{
    /// <summary>
    /// String separator for accessing the next level in a property chain.
    /// </summary>
    public const string AccessLevelSeparator = ".";

    internal const string DomainFilterLayer = "";
    internal const string ReadModelFilterLayer = "::";

    /// <summary>
    /// Splits a filter into filters that at most concern specific sub-properties. The higher the index of the sub-property in the list the more weight it has.
    /// A filter will always be considered to concern the highest weight among its leaves. Guarantees to keep the filter semantic if all the result filters are connected with a logical and.
    /// </summary>
    /// <param name="names">List of the names of sub-properties</param>
    /// <param name="domainAccessors">Domain accessors if they exist.</param>
    /// <returns>
    /// A dictionary pointing from the index of the sub-property in the input list to the biggest filter-subset that concerns it.
    /// </returns>
    public Dictionary<int, IFilter> SplitFilter(List<string> names, Dictionary<string, (LambdaExpression, bool)>? domainAccessors = null);

    /// <summary>
    /// Creates an expression tree from this filter .
    /// </summary>
    /// <param name="accessors">Mapping of parameter name -> accessor expression.</param>
    public LambdaExpression ToExpressionTree(Dictionary<string, (LambdaExpression, bool)> accessors);
    
    internal int GetDeepestSubstringIndex(List<string> names, ICollection<string>? domainAccessorKeys = null);
}