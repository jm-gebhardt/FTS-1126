namespace Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;

/// <summary>
/// Signifies that this property is part of the key of this entity.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class KeyProperty : Attribute;