using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// A load restriction.
/// </summary>
[PublicAPI]
public class LoadRestriction
{
    /// <inheritdoc cref="LoadRestriction"/>
    public LoadRestriction(bool loaded, bool unloaded, string[]? loadSetNames = null)
    {
        Loaded = loaded;
        Unloaded = unloaded;
        LoadSetNames = loadSetNames;
    }

    /// <summary>
    /// Indicates if the edge can be traversed with a load.
    /// </summary>
    public bool Loaded { get; set; }

    /// <summary>
    /// Names of the load sets allowed on this edge. Optional.
    /// </summary>
    public string[]? LoadSetNames { get; set; }

    /// <summary>
    /// Indicates if the edge can be traversed without a load.
    /// </summary>
    public bool Unloaded { get; set; }
}
