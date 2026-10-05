using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.Logging;
using Moq;
using AgvStatusGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvStatusGenerator;

// ReSharper disable MemberCanBePrivate.Global
namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

public class ChargingPointManagerTestFixture
{
    private static readonly string[] _sources = { "SRC1", "SRC2", "SRC3", "CRG1" };
    private static readonly string[] _destinations = { "DST1", "DST2", "DST3", "CRG1" };
    private static readonly string _charge = _sources[3];
    private static readonly string _node01 = _sources[0];
    private static readonly string _node02 = _sources[1];
    private static readonly string _node03 = _destinations[0];
    private static readonly string _node04 = _destinations[1];
    private static readonly string _node05 = _sources[2];
    private static readonly string _node06 = _destinations[2];
    public const string AgvSerialNumber = "AGV001";
    public const string LockedChargerName = "Charge-Locked";

    public AgvGraph AgvGraph { get; private set; } = null!;

    public TransportOrder.Domain.Models.Agv.Agv Agv { get; private set; } = null!;

    public IChargingPointManager ChargingPointManager { get; private set; } = null!;

    private readonly Mock<IAgvDb> _agvDb = new();

    public Mock<INodeRepository> NodeRepository { get; } = new();

    public Mock<IRouteService> RouteService { get; } = new();

    private readonly GraphNode[] _nodes =
    {
        NodeGenerator.GenerateNode(AgvGraph.NodeType.Charge, -2f, -2f, nodeName: _charge),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 0f, nodeName: _node01),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 4f, 0f, nodeName: _node02),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 3f, nodeName: _node03),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 4f, 3f, nodeName: _node04),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 6f, 9f, nodeName: _node05),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 13f, 5f, nodeName: _node06)
    };

    private readonly GraphEdge[] _edges =
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
        GraphEdge.CreateDirect(_node04.ToGuid(), _node03.ToGuid(), 4f)
        //no edges for node 5 and 6 on purpose so that there is no route to this stations
    };

    public ChargingPointManagerTestFixture WithGraphRepository(bool withChargerNode)
    {
        var correlationId = AgvSerialNumber.ToGuid();

        var nodes = _nodes;
        var edges = _edges;
        if (withChargerNode)
        {
            AgvGraph = AgvGraphGenerator.Generate(nodes, edges, correlationId);
        }
        else
        {
            nodes = nodes.Where(n => !n.IsCharger()).ToArray();
            edges = edges.Where(edge => !(nodes.FirstOrDefault(node => node.Id == edge.FromNodeId)?.IsCharger() ?? false)
                                        || !(nodes.FirstOrDefault(node => node.Id == edge.ToNodeId)?.IsCharger() ?? false)).ToArray();

            AgvGraph = AgvGraphGenerator.Generate(nodes, edges, correlationId);
        }

        SetupGraphRepository(AgvGraph);
        return this;
    }

    public ChargingPointManagerTestFixture WithNodeLocker(bool success)
    {
        NodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(success);
        return this;
    }

    public ChargingPointManagerTestFixture WithNode(GraphNode node)
    {
        NodeRepository.Setup(x => x.GetNodeAsync(It.IsAny<Guid>())).ReturnsAsync(node);
        return this;
    }

    public ChargingPointManagerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv? agv = null)
    {
        if (agv == null)
        {
            var correlationId = AgvSerialNumber.ToGuid();
            var agvStatus = AgvStatusGenerator.Generate(correlationId, "Idle", true, true, 10);

            Agv = AgvGenerator.GenerateRawAgv(correlationId, AgvSerialNumber, agvStatus);
        }
        else
        {
            Agv = agv;
        }

        return this;
    }

    public ChargingPointManagerTestFixture WithChargingPointManager()
    {
        _agvDb.AddAgvData((AgvData?)null);
        ChargingPointManager = new ChargingPointManager(_agvDb.Object, NodeRepository.Object, RouteService.Object, new Mock<ILogger<ChargingPointManager>>().Object, new SemaphoreProvider<IChargingSemaphore>(new Mock<ILogger<SemaphoreProvider<IChargingSemaphore>>>().Object));
        return this;
    }

    public ChargingPointManagerTestFixture WithAvailableNodes(List<GraphNode> availableNodes)
    {
        _agvDb.Setup(x => x.Graphs.GetNodesAsync(Agv.CorrelationId, It.IsAny<Expression<Func<GraphNode, bool>>>()))
            .ReturnsAsync(new Func<Guid, Expression<Func<GraphNode, bool>>, IEnumerable<GraphNode>>((_, predicate) => availableNodes.AsQueryable().Where(predicate)));
        foreach (var availableNode in availableNodes)
        {
            _agvDb.Setup(x => x.Graphs.GetNodeAsync(availableNode.Id))
                .ReturnsAsync(availableNodes.First(n => n.Id == availableNode.Id));
        }
        return this;
    }

    public ChargingPointManagerTestFixture WithLowestCostNode(IList<GraphNode> availableNodes, GraphNode node)
    {
        RouteService.WithRouteLowestCostNode(Agv, availableNodes, node);
        return this;
    }

    public static GraphNode GenerateLockedChargingNode()
    {
        var node = NodeGenerator.GenerateChargingNode(LockedChargerName);
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);
        return node;
    }

    public void SetupGraphRepository(AgvGraph graph)
    {
        _agvDb.AddAgvGraph(graph);
        _agvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>>()))
            .ReturnsAsync(new Func<Guid, Expression<Func<GraphNode, bool>>, IEnumerable<GraphNode>>((_, predicate) => graph.Nodes.AsQueryable().Where(predicate)));
    }
}