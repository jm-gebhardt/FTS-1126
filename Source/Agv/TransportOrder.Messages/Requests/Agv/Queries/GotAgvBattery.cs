using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for the GetAgvBattery request.
/// </summary>
/// <param name="AgvBatteryStatus"></param>
public record GotAgvBattery(AgvBatteryStatus AgvBatteryStatus);