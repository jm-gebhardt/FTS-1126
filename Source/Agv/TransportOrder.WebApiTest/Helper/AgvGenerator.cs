using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;

public static class AgvGenerator
{
    [UsedImplicitly]
    public static Domain.Models.Agv.Agv GenerateIdleAgv(int index = 0, string? lastNodeId = null, bool isLocked = false)
    {
        var agv = GenerateAgv(AgvState.Idle, index, lastNodeId, isLocked: isLocked);
        return agv;
    }

    [UsedImplicitly]
    public static Domain.Models.Agv.Agv GenerateIdleAgv(AgvStatus agvStatus, int index = 0)
    {
        return GenerateAgv(AgvState.Idle, index, null, agvStatus);
    }

    [UsedImplicitly]
    public static Domain.Models.Agv.Agv GenerateChargingAgv(int index = 0, string? lastNodeId = null)
    {
        return GenerateAgv(AgvState.Busy, index, lastNodeId, charging: true);
    }

    private static Domain.Models.Agv.Agv GenerateAgv(
        AgvState state,
        int index,
        string? lastNodeId,
        AgvStatus? agvStatus = null,
        bool charging = false,
        bool isLocked = false
    )
    {
        string agvSerialNumber = GetAgvSerialNumber(index);
        var correlationId = agvSerialNumber.ToGuid();
        agvStatus ??= new AgvStatus(correlationId, "orderId", true, true, 1, charging, false, lastNodeId);
        var agv = new Domain.Models.Agv.Agv(correlationId, agvSerialNumber, agvStatus);
        return SetAgvState(state, isLocked, agv);
    }

    private static string GetAgvSerialNumber(int index)
    {
        return $"AGV{index:D3}";
    }

    public static Domain.Models.Agv.Agv SetAgvState(AgvState state, bool isLocked, Domain.Models.Agv.Agv agv)
    {
        switch (state)
        {
            case AgvState.Busy:
                agv.TransitionToDiscovered(true);
                agv.TransitionToIdle();
                agv.TryTransitionToBusy(isLocked);
                break;

            case AgvState.Discovered:
                agv.TransitionToDiscovered(true);
                break;

            case AgvState.Idle:
                agv.TransitionToDiscovered(true);
                agv.TransitionToIdle();
                break;
        }
        return agv;
    }
}