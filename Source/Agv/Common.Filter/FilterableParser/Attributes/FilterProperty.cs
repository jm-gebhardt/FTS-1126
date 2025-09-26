namespace Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;

/// <summary>
/// Signifies that this property can be filtered.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FilterProperty : Attribute;