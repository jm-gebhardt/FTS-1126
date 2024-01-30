using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;

public static class AgvInformationGenerator
{
    private static readonly string[] _sources = { "SRC1", "SRC2", "CRG2", "CRG1", "CRG3" };
    private static readonly string[] _destinations = { "DST1", "DST2", "DST3", "DST2", "DST3" };
    private static readonly string[] _agvIds = { "AGV001", "AGV002", "AGV003", "AGV004", "AGV005" };
    private static readonly string[] _orders = { "ORDER001", "ORDER002", "ORDER003", "ORDER004", "ORDER005" };
    private static readonly string[] _agvStates = { "Idle", "Busy", "Busy", "Ready", "Ready" };
    private static readonly bool[] _chargingState = { false, false, true, false, false };
    private static readonly string _charge = _sources[3];
    private static readonly string _node01 = _sources[0];
    private static readonly string _node02 = _sources[1];
    private static readonly string _node03 = _destinations[0];
    private static readonly string _node04 = _destinations[1];
    private static readonly string _charge2 = _sources[2];

    public static AgvStatusReceived? GetAgvStatusReceived(AgvStatus? agvStatus)
    {
        return agvStatus != null ? new AgvStatusReceived(agvStatus.CorrelationId, "agvId", agvStatus) : null;
    }

    public static AgvInformation GetAgvInformation(int i = 0, Guid agvCorrelationId = default)
    {
        string agvId = _agvIds[i];
        string order = _orders[i];
        string state = _agvStates[i];
        var correlationId = agvCorrelationId != default ? agvCorrelationId : agvId.ToGuid();
        var actionGroupId = Guid.Empty;
        var agvStatus = new AgvStatus(correlationId, order, state == "Busy", true, 25 * i, _chargingState[i], 48, _chargingState[i] ? 20 : -10, 20, false, _sources[i]);
        var agvGraph = new AgvGraph(correlationId, GetNodes(), GetEdges());
        var agvAction = new AgvAction($"Action#{agvId}".ToGuid(), ActionType.Pick, "Drop5", actionGroupId);
        var agvInformation = GetAgvInformation(agvStatus, agvGraph, agvAction);
        return agvInformation;
    }

    public static AgvInformation GetAgvInformation(AgvStatus agvStatus, AgvGraph agvGraph, AgvAction agvAction)
    {
        var agvInformation = new AgvInformation { AgvStatusReceived = GetAgvStatusReceived(agvStatus), AgvGraph = agvGraph, AgvAction = agvAction };
        return agvInformation;
    }

    private static GraphEdge[] GetEdges()
    {
        return new[]
        {
            GraphEdge.CreateDirect(_charge, _node01, 2f),
            GraphEdge.CreateDirect(_node01, _charge, 2f),
            GraphEdge.CreateDirect(_node01, _node02, 4f),
            GraphEdge.CreateDirect(_node01, _node03, 3f),
            GraphEdge.CreateDirect(_node01, _node04, 5f),
            GraphEdge.CreateDirect(_node02, _node01, 4f),
            GraphEdge.CreateDirect(_node02, _node04, 6f),
            GraphEdge.CreateDirect(_node03, _node01, 3f),
            GraphEdge.CreateDirect(_node03, _node04, 4f),
            GraphEdge.CreateDirect(_node04, _node01, 5f),
            GraphEdge.CreateDirect(_node04, _node02, 6f),
            GraphEdge.CreateDirect(_node04, _node03, 4f),
            GraphEdge.CreateDirect(_charge2, _node04, 4f)
        };
    }

    private static GraphNode[] GetNodes()
    {
        return new[]
        {
            new GraphNode(_sources[3]) { Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 0f },
            new GraphNode(_sources[0]) { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
            new GraphNode(_sources[1]) { Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f },
            new GraphNode(_destinations[0]) { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 3f, Z = 0f },
            new GraphNode(_destinations[1]) { Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 3f, Z = 0f },
            new GraphNode(_sources[2]) { Type = AgvGraph.NodeType.Charge, X = 6f, Y = 9f, Z = 0f },
            new GraphNode(_destinations[2]) { Type = AgvGraph.NodeType.PickDrop, X = 13f, Y = 5f, Z = 0f }
        };
    }
}