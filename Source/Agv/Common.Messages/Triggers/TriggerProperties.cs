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
    }

    /// <summary>
    /// The id of the trigger.
    /// </summary>
    public Guid Id  => $"{Urn}:{Version}".ToGuid();
    
    /// <summary>
    /// The unique resource name : 
    /// </summary>
    public string Urn { get; init; }

    /// <summary>
    /// Trigger name as displayed in the ui.
    /// </summary>
    public string DisplayName { get; init; }

    /// <summary>
    /// Version of the trigger : "0.0.1"
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Description of the triggers functionality
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// If set, the maximum allowed trigger instances in the graph 
    /// </summary>
    public int? InstanceMaximum { get; init; }
}
