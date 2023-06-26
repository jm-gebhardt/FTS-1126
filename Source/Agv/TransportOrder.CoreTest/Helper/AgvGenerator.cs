using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvGenerator
{
    public static IAgv GenerateIdleAgv(int index = 0, string? lastNodeId = null, AgvGraph? agvGraph = null)
    {
        return GenerateAgv(nameof(AgvStateMachine.Idle), index, lastNodeId, null, agvGraph);
    }

    [UsedImplicitly]
    public static IAgv GenerateIdleAgv(AgvStatus agvStatus, int index = 0)
    {
        return GenerateAgv(nameof(AgvStateMachine.Idle), index, null, agvStatus);
    }

    public static IAgv GenerateChargingAgv(int index = 0, string? lastNodeId = null)
    {
        return GenerateAgv(nameof(AgvStateMachine.Charging), index, lastNodeId, charging: true);
    }

    private static IAgv GenerateAgv(
        string state,
        int index,
        string? lastNodeId,
        AgvStatus? agvStatus = null,
        AgvGraph? agvGraph = null,
        bool charging = false)
    {
        string agvId = GetAgvId(index);
        var correlationId = agvId.ToGuid();
        agvStatus ??= new AgvStatus(correlationId, "orderId", true, true, 1, charging, false, lastNodeId);
        return new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph);
    }

    private static string GetAgvId(int index) => $"AGV{index:D3}";
}