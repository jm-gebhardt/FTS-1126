using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for the GetAgvBattery request.
/// </summary>
/// <param name="AgvBatteryStatus">The status of the AGV's battery.</param>
/// <param name="AgvSettings">The settings for the AGV.</param>
public record GotAgvBattery(AgvBatteryStatus AgvBatteryStatus, [UsedImplicitly] AgvSettings AgvSettings);