using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvGenerator
{
    private static string GetAgvId(int index) => $"AGV{index:D3}";

    public static IAgv GenerateIdleAgv(int index = 0, string? lastNodeId = "lastNodeId")
    {
        string agvId = GetAgvId(index);
        var correlationId = agvId.ToGuid();
        var agvStatus = new AgvStatus(correlationId, "orderId",false, true, 100, false, false, lastNodeId);
        return new AgvSaga(correlationId, agvId, nameof(AgvStateMachine.Idle), agvStatus);
    }

    [UsedImplicitly]
    public static IAgv GenerateIdleAgv(AgvStatus? agvStatus, int index = 0)
    {
        string agvId = GetAgvId(index);
        var correlationId = agvId.ToGuid();
        return new AgvSaga(correlationId, agvId, nameof(AgvStateMachine.Idle), agvStatus);
    }

    public static IAgv GenerateChargingAgv(int index = 0, string? lastNodeId = "lastNodeId")
    {
        string agvId = GetAgvId(index);
        var correlationId = agvId.ToGuid();
        var agvStatus = new AgvStatus(correlationId, "orderId",true, true, 1, true, false, lastNodeId);
        return new AgvSaga(correlationId, agvId, nameof(AgvStateMachine.Charging), agvStatus);
    }
}