using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs.Filters;

public static class AgvBatteryStatusDtoGenerator
{
    public static AgvBatteryStatusDto Generate(DateTime messageDate)
    {
        return new AgvBatteryStatusDto { BatteryCharge = 50, MessageCreatedDate = messageDate };
    }
}