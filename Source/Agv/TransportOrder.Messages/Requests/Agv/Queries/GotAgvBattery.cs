using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for the GetAgvBattery request.
/// </summary>
/// <param name="AgvBatteryStatus"></param>
public record GotAgvBattery(AgvBatteryStatus AgvBatteryStatus);