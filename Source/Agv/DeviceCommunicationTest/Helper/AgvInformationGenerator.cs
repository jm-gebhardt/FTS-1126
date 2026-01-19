using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;

namespace Gebhardt.Acc.DeviceCommunicationTest.Helper;

public static class AgvInformationGenerator
{
    private static readonly string[] _sources = { "SRC1", "SRC2", "CRG2", "CRG1", "CRG3" };
    private static readonly string[] _destinations = { "DST1", "DST2", "DST3", "DST2", "DST3" };
    private static readonly string[] _agvSerialNumbers = { "AGV001", "AGV002", "AGV003", "AGV004", "AGV005" };
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
        string agvSerialNumber = _agvSerialNumbers[i];
        string order = _orders[i];
        string state = _agvStates[i];
        var correlationId = agvCorrelationId != Guid.Empty ? agvCorrelationId : agvSerialNumber.ToGuid();
        var actionGroupId = Guid.Empty;
        var agvStatus = AgvStatusGenerator.Generate(agvCorrelationId: correlationId, orderId: order, busy: state == "Busy", batteryCharge: 25 * i, charging: _chargingState[i], lastNodeName: _sources[i], totalDistance: 42f, totalLifetime: 30f);
        var agvGraph = AgvGraphGenerator.Generate(GetNodes(), GetEdges(), correlationId);
        var agvAction = AgvActionGenerator.GenerateAction(targetNodeId: "Drop5", actionType: ActionType.Pick, correlationId: $"Action#{agvSerialNumber}".ToGuid(), parentId: actionGroupId);
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
            GraphEdge.CreateDirect(_charge.ToGuid(), _node01.ToGuid(), 2f),
            GraphEdge.CreateDirect(_node01.ToGuid(), _charge.ToGuid(), 2f),
            GraphEdge.CreateDirect(_node01.ToGuid(), _node02.ToGuid(), 4f),
            GraphEdge.CreateDirect(_node01.ToGuid(), _node03.ToGuid(), 3f),
            GraphEdge.CreateDirect(_node01.ToGuid(), _node04.ToGuid(), 5f),
            GraphEdge.CreateDirect(_node02.ToGuid(), _node01.ToGuid(), 4f),
            GraphEdge.CreateDirect(_node02.ToGuid(), _node04.ToGuid(), 6f),
            GraphEdge.CreateDirect(_node03.ToGuid(), _node01.ToGuid(), 3f),
            GraphEdge.CreateDirect(_node03.ToGuid(), _node04.ToGuid(), 4f),
            GraphEdge.CreateDirect(_node04.ToGuid(), _node01.ToGuid(), 5f),
            GraphEdge.CreateDirect(_node04.ToGuid(), _node02.ToGuid(), 6f),
            GraphEdge.CreateDirect(_node04.ToGuid(), _node03.ToGuid(), 4f),
            GraphEdge.CreateDirect(_charge2.ToGuid(), _node04.ToGuid(), 4f)
        };
    }

    private static GraphNode[] GetNodes()
    {
        return new[]
        {
            NodeGenerator.GenerateNode(nodeName: _sources[3], nodeType: AgvGraph.NodeType.Charge, x: -2f, y: -2f, z: 0f),
            NodeGenerator.GenerateNode(nodeName: _sources[0], nodeType: AgvGraph.NodeType.PickDrop, x: 0f, y: 0f, z: 0f),
            NodeGenerator.GenerateNode(nodeName: _sources[1], nodeType: AgvGraph.NodeType.PickDrop, x: 4f, y: 0f, z: 0f),
            NodeGenerator.GenerateNode(nodeName: _destinations[0], nodeType: AgvGraph.NodeType.PickDrop, x: 0f, y: 3f, z: 0f),
            NodeGenerator.GenerateNode(nodeName: _destinations[1], nodeType: AgvGraph.NodeType.PickDrop, x: 4f, y: 3f, z: 0f),
            NodeGenerator.GenerateNode(nodeName: _sources[2], nodeType: AgvGraph.NodeType.Charge, x: 6f, y: 9f, z: 0f),
            NodeGenerator.GenerateNode(nodeName: _destinations[2], nodeType: AgvGraph.NodeType.PickDrop, x: 13f, y: 5f, z: 0f)
        };
    }
}