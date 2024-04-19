
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;

/// <summary>
/// Provides a dto for action duration statistics.
/// </summary>
/// <param name="ActionType">The type of action.</param>
/// <param name="MedianDuration">The median duration [s].</param>
/// <param name="MinDuration">The minimum duration [s].</param>
/// <param name="MaxDuration">The maximum duration [s].</param>
public record ActionDurationStatisticsDto(ActionType ActionType, double MedianDuration, double MinDuration, double MaxDuration);