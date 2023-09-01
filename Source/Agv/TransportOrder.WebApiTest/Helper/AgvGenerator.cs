using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using JetBrains.Annotations;
using TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;

public static class AgvGenerator
{
    [UsedImplicitly]
    public static IAgv GenerateIdleAgv(int index = 0, string? lastNodeId = null, bool isLocked = false)
    {
        var agv = GenerateAgv(nameof(AgvStateMachine.Idle), index, lastNodeId);
        agv.IsLocked = isLocked;
        return agv;
    }

    [UsedImplicitly]
    public static IAgv GenerateIdleAgv(AgvStatus agvStatus, int index = 0)
    {
        return GenerateAgv(nameof(AgvStateMachine.Idle), index, null, agvStatus);
    }

    [UsedImplicitly]
    public static IAgv GenerateChargingAgv(int index = 0, string? lastNodeId = null)
    {
        return GenerateAgv(nameof(AgvStateMachine.Charging), index, lastNodeId, charging: true);
    }

    private static IAgv GenerateAgv(
        string state,
        int index,
        string? lastNodeId,
        AgvStatus? agvStatus = null,
        bool charging = false)
    {
        string agvId = GetAgvId(index);
        var correlationId = agvId.ToGuid();
        agvStatus ??= new AgvStatus(correlationId, "orderId", true, true, 1, charging, false, lastNodeId);
        return new AgvSaga(correlationId, agvId, state, agvStatus);
    }

    private static string GetAgvId(int index) => $"AGV{index:D3}";
}