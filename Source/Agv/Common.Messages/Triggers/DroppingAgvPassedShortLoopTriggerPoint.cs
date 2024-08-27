using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when an agv passed the trigger point on the short loop that is going to drop at 1050.
/// </summary>
/// <param name="AgvData">The agv data.</param>
/// <param name="ActionGroup">The current action group.</param>
/// <param name="ShouldNotPickNext">The agv should not pick after dropping.</param>
[PublicAPI]
public record DroppingAgvPassedShortLoopTriggerPoint(AgvData AgvData, ActionGroup? ActionGroup, bool ShouldNotPickNext);
