using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the <see cref="GetAgvsBattery"/> request.
/// </summary>
public record GotAgvsBattery(List<(AgvBatteryStatus, AgvSettings)> AgvBatteryStatus);