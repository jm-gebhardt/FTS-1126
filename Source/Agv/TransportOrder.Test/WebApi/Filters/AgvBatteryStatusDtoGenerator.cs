using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Filters;

public static class AgvBatteryStatusDtoGenerator
{
    public static AgvBatteryStatusDto Generate(DateTime messageDate)
    {
        return new AgvBatteryStatusDto { BatteryCharge = 50, MessageCreatedDate = messageDate };
    }
}