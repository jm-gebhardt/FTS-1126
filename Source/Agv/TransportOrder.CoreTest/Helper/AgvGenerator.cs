using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvGenerator
{
    public static TransportOrder.Domain.Models.Agv.Agv GenerateReadyAgv(int index = 0, bool isLocked = false, int batteryCharge = 100)
    {
        var agv = GenerateAgv(AgvState.Idle, index, "A", isLocked: isLocked, batteryCharge: batteryCharge);
        return agv;
    }

    public static TransportOrder.Domain.Models.Agv.Agv GenerateBusyAgv(int index = 0, bool isLocked = false, int batteryCharge = 100, bool hasError = false)
    {
        var agv = GenerateAgv(AgvState.Busy, index, "A", isLocked: isLocked, batteryCharge: batteryCharge, hasError: hasError);
        return agv;
    }

    public static TransportOrder.Domain.Models.Agv.Agv GenerateIdleAgv(int index = 0, string? lastNodeId = "node", bool isLocked = false, int batteryCharge = 100, bool hasError = false)
    {
        var agv = GenerateAgv(AgvState.Idle, index, lastNodeId, isLocked: isLocked, batteryCharge: batteryCharge, hasError: hasError);
        return agv;
    }

    public static IEnumerable<TransportOrder.Domain.Models.Agv.Agv> GenerateIdleAgvs(int count, string? lastNodeId = null, bool isLocked = false, int batteryCharge = 100, bool hasError = false)
    {
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            yield return GenerateAgv(AgvState.Idle, ++index, lastNodeId, isLocked: isLocked, batteryCharge: batteryCharge, hasError: hasError);
        }
    }

    [UsedImplicitly]
    public static TransportOrder.Domain.Models.Agv.Agv GenerateIdleAgv(AgvStatus agvStatus, int index = 0)
    {
        return GenerateAgv(AgvState.Idle, index, null, agvStatus);
    }

    [UsedImplicitly]
    public static TransportOrder.Domain.Models.Agv.Agv GenerateChargingAgv(int index = 0, string? lastNodeId = "C-1", bool isLocked = false, int batteryCharge = 100)
    {
        return GenerateAgv(AgvState.Busy, index, lastNodeId, charging: true, isLocked: isLocked, batteryCharge: batteryCharge, orderId: string.Empty);
    }

    [UsedImplicitly]
    public static TransportOrder.Domain.Models.Agv.Agv GenerateRawAgv(string serialNumber = "agvId", string? agvName = null)
    {
        return GenerateAgv(serialNumber, agvName);
    }

    private static TransportOrder.Domain.Models.Agv.Agv GenerateAgv(
        AgvState agvState,
        int index,
        string? lastNodeId,
        AgvStatus? agvStatus = null,
        bool charging = false,
        bool isLocked = false,
        int batteryCharge = 100,
        string orderId = "orderId",
        bool hasError = false
    )
    {
        string agvSerialNumber = GetAgvSerialNumber(index);
        var correlationId = agvSerialNumber.ToGuid();
        agvStatus ??= new AgvStatus(correlationId, orderId, false, true, batteryCharge, charging, false, lastNodeId);
        var agv = new TransportOrder.Domain.Models.Agv.Agv(correlationId, Guid.NewGuid(), agvSerialNumber, agvStatus, isLocked)
        {
            HasError = hasError
        };
        return SetAgvState(agvState, agv);
    }

    private static TransportOrder.Domain.Models.Agv.Agv GenerateAgv(string serialNumber, string? agvName = null)
    {
        var agv = new TransportOrder.Domain.Models.Agv.Agv(serialNumber, Guid.NewGuid(), agvName);
        return agv;
    }

    public static TransportOrder.Domain.Models.Agv.Agv SetAgvState(AgvState agvState, TransportOrder.Domain.Models.Agv.Agv agv)
    {
        switch (agvState)
        {
            case AgvState.Busy:
                agv.TransitionToDiscovered(true);
                agv.TransitionToIdle();
                agv.TryTransitionToBusy(true);
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

    private static string GetAgvSerialNumber(int index)
    {
        return $"AGV{index:D3}";
    }
}