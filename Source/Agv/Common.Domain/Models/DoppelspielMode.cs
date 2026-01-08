using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Doppelspiel modes supported by the system.
/// </summary>
[PublicAPI]
public enum DoppelspielMode
{
    /// <summary>No Doppelspiel.</summary>
    None,

    /// <summary>Doppelspiel depending on next pick availability.</summary>
    Order,

    /// <summary>Always Doppelspiel.</summary>
    Always,

    /// <summary>Doppelspiel depending on next pick availability and if there is no queue head.</summary>
    OrderOrQueueHead
}