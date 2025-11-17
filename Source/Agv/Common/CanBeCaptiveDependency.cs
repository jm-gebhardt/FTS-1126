using JetBrains.Annotations;

namespace Gebhardt.Acc.Common;

/// <summary>
/// Signifies that this transient service may be injected into a singleton.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
public class CanBeCaptiveDependency : Attribute;