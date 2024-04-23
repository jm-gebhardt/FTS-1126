using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Provides an interface to label action group related events.
/// </summary>
public interface IActionGroupEvent
{
    /// <summary>
    /// The action group
    /// </summary>
    [UsedImplicitly]
    ActionGroup ActionGroup { get; }
}