using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Represents a validation result.
/// </summary>
/// <param name="Type">The type of the validated resource.</param>
/// <param name="ResourceId">The id of the validated resource (if applicable).</param>
/// <param name="Severity">The severity of the result.</param>
/// <param name="Description">The result description-</param>
[UsedImplicitly]
public record LifValidationResult(ResourceType Type, string? ResourceId, Severity Severity, string Description);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Type of evaluated resource.
/// </summary>
public enum ResourceType
{
    Station,
    Layout,
    Node,
    Edge,
    MetaInformation,
    File,
    General
}

/// <summary>
/// Result severity.
/// </summary>
[PublicAPI]
public enum Severity
{
    Info,
    Warn,
    Err
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
