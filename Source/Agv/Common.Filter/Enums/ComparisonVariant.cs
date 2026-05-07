namespace Gebhardt.Acc.Common.Filter.Enums;

/// <summary>
/// Defines a comparison operation.
/// </summary>
public enum ComparisonVariant
{
    /// <summary>
    /// &lt;=
    /// </summary>
    Leq,

    /// <summary>
    /// &gt;=
    /// </summary>
    Geq,
    
    /// <summary>
    /// &lt;
    /// </summary>
    Lt,

    /// <summary>
    /// &gt;
    /// </summary>
    Gt,

    /// <summary>
    /// =
    /// </summary>
    Equals,

    /// <summary>
    /// Substring match with placeholders.
    /// </summary>
    Like,

    /// <summary>
    /// !=
    /// </summary>
    Neq,

    /// <summary>
    /// Negated substring match with placeholders.
    /// </summary>
    NotLike
}