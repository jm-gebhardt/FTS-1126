namespace Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;

/// <summary>
/// Signifies that this property has a conversion defined.<br/>
/// Properties with conversions can never be filtered before materializing.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class HasConversion : Attribute;