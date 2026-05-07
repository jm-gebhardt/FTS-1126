using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;

/// <summary>
/// Specifies that this filterable property reads from a differently-named property on the domain model,
/// allowing properties with value converters to be filtered via a typed projection.
/// </summary>
/// <param name="sourcePropertyName">The name of the property on the domain model to use as source.</param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Property)]
public sealed class MapsFromPropertyAttribute(string sourcePropertyName) : Attribute
{
    /// <summary>The name of the source property on the domain model.</summary>
    public string SourcePropertyName { get; } = sourcePropertyName;
}
