using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;

/// <summary>
/// Scope in which an action type may be used.
/// </summary>
[PublicAPI]
public enum ActionScope
{
    /// <summary>Usable as an instant action.</summary>
    Instant,

    /// <summary>Usable on nodes.</summary>
    Node,

    /// <summary>Usable on edges.</summary>
    Edge
}
