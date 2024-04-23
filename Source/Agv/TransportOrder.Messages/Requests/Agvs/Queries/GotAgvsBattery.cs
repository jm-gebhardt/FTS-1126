using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the <see cref="GetAgvsBattery"/> request.
/// </summary>
public record GotAgvsBattery(List<AgvBatteryStatus> AgvBatteryStatus);