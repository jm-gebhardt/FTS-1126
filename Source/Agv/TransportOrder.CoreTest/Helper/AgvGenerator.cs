using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvGenerator
{
    public static TransportOrder.Domain.Models.Agv.Agv GenerateIdleAgv(int index = 0, string? lastNodeId = null, bool isLocked = false, int batteryCharge = 100)
    {
        var agv = GenerateAgv(AgvState.Idle, index, lastNodeId, isLocked: isLocked, batteryCharge: batteryCharge);
        return agv;
    }


    public static IEnumerable<TransportOrder.Domain.Models.Agv.Agv> GenerateIdleAgvs(int count, string? lastNodeId = null, bool isLocked = false, int batteryCharge = 100)
    {
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            yield return GenerateAgv(AgvState.Idle, ++index, lastNodeId, isLocked: isLocked, batteryCharge: batteryCharge);
        }
    }

    [UsedImplicitly]
    public static TransportOrder.Domain.Models.Agv.Agv GenerateIdleAgv(AgvStatus agvStatus, int index = 0)
    {
        return GenerateAgv(AgvState.Idle, index, null, agvStatus);
    }

    [UsedImplicitly]
    public static TransportOrder.Domain.Models.Agv.Agv GenerateChargingAgv(int index = 0, string? lastNodeId = null, bool isLocked = false, int batteryCharge = 100)
    {
        return GenerateAgv(AgvState.Charging, index, lastNodeId, charging: true, isLocked:isLocked, batteryCharge: batteryCharge, orderId: string.Empty);
    }


    private static TransportOrder.Domain.Models.Agv.Agv GenerateAgv(
        AgvState agvState,
        int index,
        string? lastNodeId,
        AgvStatus? agvStatus = null,
        bool charging = false,
        bool isLocked = false,
        int batteryCharge = 100,
        string orderId = "orderId"
    )
    {
        string agvId = GetAgvId(index);
        var correlationId = agvId.ToGuid();
        agvStatus ??= new AgvStatus(correlationId, orderId, agvState != AgvState.Idle, true, batteryCharge, charging, false, lastNodeId);
        var agv = new TransportOrder.Domain.Models.Agv.Agv(correlationId, agvId, agvStatus, isLocked);
        return SetAgvState(agvState, isLocked, agv);
    }

    public static TransportOrder.Domain.Models.Agv.Agv SetAgvState(AgvState agvState, bool isLocked, TransportOrder.Domain.Models.Agv.Agv agv)
    {
        switch (agvState)
        {
            case AgvState.Busy:
                agv.TransitionToDiscovered(isLocked);
                agv.TransitionToIdle();
                agv.TryTransitionToBusy(isLocked);
                break;

            case AgvState.Discovered:
                agv.TransitionToDiscovered(true);
                break;

            case AgvState.Idle:
                agv.TransitionToDiscovered(isLocked);
                agv.TransitionToIdle();
                break;

            case AgvState.Charging:
                agv.TransitionToDiscovered(isLocked);
                agv.TransitionToIdle();
                agv.TransitionToCharging();
                break;
        }
        return agv;
    }

    private static string GetAgvId(int index) => $"AGV{index:D3}";
}