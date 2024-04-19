using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents the response to a <see cref="GetActionGroupStatistics"/> request.
/// </summary>
/// <param name="DurationStatistics">The duration statistics for all actions.</param>
public record GotActionGroupStatistics(ICollection<DurationStatistics> DurationStatistics);

/// <summary>
/// Represents statistics of action durations.
/// </summary>
/// <param name="ActionType">The type of action.</param>
/// <param name="MedianDuration">The median duration [s].</param>
/// <param name="MinDuration">The minimum duration [s].</param>
/// <param name="MaxDuration">The maximum duration [s].</param>
public record DurationStatistics(ActionType ActionType, double MedianDuration, double MinDuration, double MaxDuration);