using System.Linq.Expressions;

namespace Gebhardt.Acc.Common.Filter.FilterBuilder;

/// <summary>
/// 
/// </summary>
public interface IFilterLeaf
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="accessors"></param>
    /// <returns></returns>
    LambdaExpression ToExpressionTree(Dictionary<string, LambdaExpression> accessors);

    /// <summary>
    /// 
    /// </summary>
    string Name { get; }
}