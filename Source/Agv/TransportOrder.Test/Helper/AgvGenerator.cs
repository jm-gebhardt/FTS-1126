using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class AgvGenerator
{
    public static Agv GenerateBusyAgv(int index = 0,int batteryCharge = 100, bool hasError = false)
    {
        var agv = GenerateAgv(AgvState.Busy, index, "A", batteryCharge: batteryCharge, hasError: hasError);
        return agv;
    }

    public static Agv GenerateIdleAgv(int index = 0, string? lastNodeId = "node", int batteryCharge = 100, bool hasError = false)
    {
        var agv = GenerateAgv(AgvState.Idle, index, lastNodeId, batteryCharge: batteryCharge, hasError: hasError);
        return agv;
    }

    public static IEnumerable<Agv> GenerateIdleAgvs(int count, string? lastNodeId = null, int batteryCharge = 100, bool hasError = false)
    {
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            yield return GenerateAgv(AgvState.Idle, ++index, lastNodeId, batteryCharge: batteryCharge, hasError: hasError);
        }
    }

    [UsedImplicitly]
    public static Agv GenerateIdleAgv(AgvStatus agvStatus, int index = 0)
    {
        return GenerateAgv(AgvState.Idle, index, null, agvStatus);
    }

    [UsedImplicitly]
    public static Agv GenerateChargingAgv(int index = 0, string? lastNodeId = "C-1", int batteryCharge = 100)
    {
        return GenerateAgv(AgvState.Busy, index, lastNodeId, charging: true, batteryCharge: batteryCharge, orderId: string.Empty);
    }

    [UsedImplicitly]
    public static Agv GenerateRawAgv(string serialNumber = "agvId", string? agvName = null)
    {
        return GenerateAgv(serialNumber, agvName);
    }
    
    [UsedImplicitly]
    public static Agv GenerateRawAgv(Guid correlationId, string serialNumber, AgvStatus? agvStatus = null)
    {
        return GenerateAgv(correlationId, serialNumber, agvStatus);
    }

    private static Agv GenerateAgv(
        AgvState agvState,
        int index,
        string? lastNodeId,
        AgvStatus? agvStatus = null,
        bool charging = false,
        int batteryCharge = 100,
        string orderId = "orderId",
        bool hasError = false
    )
    {
        string agvSerialNumber = GetAgvSerialNumber(index);
        var correlationId = agvSerialNumber.ToGuid();
        agvStatus ??= new AgvStatus(correlationId, orderId, false, true, batteryCharge, charging, false, lastNodeId);
        var agv = new Agv(correlationId, Guid.NewGuid(), agvSerialNumber, agvStatus)
        {
            HasError = hasError
        };
        return SetAgvState(agvState, agv);
    }

    private static Agv GenerateAgv(string serialNumber, string? agvName = null)
    {
        var agv = new Agv(serialNumber, Guid.NewGuid(), agvName);
        return agv;
    }
    
    private static Agv GenerateAgv(Guid correlationId, string serialNumber, AgvStatus? agvStatus = null)
    {
        var agv = new Agv(correlationId, Guid.NewGuid(), serialNumber, agvStatus);
        return agv;
    }

    public static Agv SetAgvState(AgvState agvState, Agv agv, bool isLocked=true)
    {
        switch (agvState)
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

    private static string GetAgvSerialNumber(int index)
    {
        return $"AGV{index:D3}";
    }
}