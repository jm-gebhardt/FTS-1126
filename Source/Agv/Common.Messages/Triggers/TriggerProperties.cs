using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Trigger properties
/// </summary>
[PublicAPI]
public record TriggerProperties
{
    /// <inheritdoc cref="TriggerProperties"/>>
    public TriggerProperties(string urn, string displayName, string description, string version = "0.0.1", int? instancesMax = null)
    {
        Urn = urn;
        DisplayName = displayName;
        Version = version;
        Description = description;
        InstanceMaximum = instancesMax;
        Id = $"{Urn}:{Version}".ToGuid();
    }

    /// <inheritdoc cref="TriggerProperties"/>>
    public TriggerProperties()
    {
    }

    /// <summary>
    /// The id of the trigger.
    /// </summary>
    public Guid Id { get; init; } = Guid.Empty;

    /// <summary>
    /// The unique resource name : 
    /// </summary>
    public string Urn { get; init; } = string.Empty;

    /// <summary>
    /// Trigger name as displayed in the ui.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Version of the trigger : "0.0.1"
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Description of the triggers functionality
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// If set, the maximum allowed trigger instances in the graph 
    /// </summary>
    public int? InstanceMaximum { get; init; } 
}
