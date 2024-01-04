using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using AgvStatus = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.AgvStatus;

// ReSharper disable MemberCanBePrivate.Global
namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

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

    private readonly Mock<IGraphRepository> _graphRepository = new();

    private readonly Mock<IAgvDataRepository> _agvDataRepository = new();

    public Mock<INodeStateRepository> NodeStateRepository { get; } = new();

    public Mock<IRouteService> RouteService { get; } = new();

    private readonly GraphNode[] _nodes =
    {
        new(_charge) { Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 0f },
        new(_node01) { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
        new(_node02) { Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f },
        new(_node03) { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 3f, Z = 0f },
        new(_node04) { Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 3f, Z = 0f },
        new(_node05) { Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 9f, Z = 0f },
        new(_node06) { Type = AgvGraph.NodeType.PickDrop, X = 13f, Y = 5f, Z = 0f }
    };

    private readonly GraphEdge[] _edges =
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
        GraphEdge.CreateDirect(_node04, _node03, 4f)
        //no edges for node 5 and 6 on purpose so that there is no route to this stations
    };

    public AgvGraph GetAgvGraph(bool withChargerNode, Guid? correlationId = null)
    {
        GraphNode[] nodes = _nodes;
        GraphEdge[] edges = _edges;
        if (withChargerNode)
        {
            return new AgvGraph(correlationId ?? Guid.NewGuid(), nodes, edges);
        }

        nodes = nodes.Where(n => n.Type != AgvGraph.NodeType.Charge).ToArray();
        edges = edges.Where(edge => nodes.FirstOrDefault(node => node.Id == edge.FromNodeId)?.Type != AgvGraph.NodeType.Charge
                                    || nodes.FirstOrDefault(node => node.Id == edge.ToNodeId)?.Type != AgvGraph.NodeType.Charge).ToArray();
        return new AgvGraph(correlationId ?? Guid.NewGuid(), nodes, edges);
    }

    public IChargingPointManager CreateChargingPointManager()
    {
        return new ChargingPointManager(_graphRepository.Object, NodeStateRepository.Object, RouteService.Object, new Mock<ILogger<ChargingPointManager>>().Object, new SemaphoreProvider<IChargingSemaphore>(), _agvDataRepository.Object);
    }

    public void SetupNodeLocker(bool reserveSuccess, bool lockSuccess, bool unlockSuccess, Func<IQueryable<GraphNode>, IQueryable<GraphNode>>? filterFreeUnlockedNodesFunction = null)
    {
        NodeStateRepository.Setup(s => s.ReserveNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(reserveSuccess);
        NodeStateRepository.Setup(s => s.LockNodeAsync(It.IsAny<string>())).ReturnsAsync(lockSuccess);
        NodeStateRepository.Setup(s => s.UnlockNodeAsync(It.IsAny<string>())).ReturnsAsync(unlockSuccess);

        filterFreeUnlockedNodesFunction ??= nodes => nodes;
        NodeStateRepository.Setup(s => s.FilterFreeUnlockedNodes(It.IsAny<IQueryable<GraphNode>>())).Returns(filterFreeUnlockedNodesFunction);
    }

    public void SetupGraphRepository(AgvGraph graph)
    {
        _graphRepository.Setup(x => x.GetAgvGraphAsync(It.IsAny<Guid>())).ReturnsAsync(graph);
        _graphRepository.Setup(x => x.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>>()))
            .ReturnsAsync(new Func<Guid, Expression<Func<GraphNode, bool>>, IEnumerable<GraphNode>>((_, predicate) => graph.Nodes.AsQueryable().Where(predicate)));
    }

    public TransportOrder.Domain.Models.Agv.Agv GetAgv(Guid correlationId, string agvId, string order, string? lastNodeId = null)
    {
        var agvStatus = new AgvStatus(correlationId, order, true, true, 10, false, false, lastNodeId);

        return new TransportOrder.Domain.Models.Agv.Agv(correlationId, agvId, agvStatus);
    }

    public ChargingPointManagerTestFixture WithAvailableNodes(TransportOrder.Domain.Models.Agv.Agv agv, List<GraphNode> availableNodes)
    {
        _graphRepository.Setup(x => x.GetNodesAsync(agv.CorrelationId, It.IsAny<Expression<Func<GraphNode, bool>>>()))
            .ReturnsAsync(new Func<Guid, Expression<Func<GraphNode, bool>>, IEnumerable<GraphNode>>((_, predicate) => availableNodes.AsQueryable().Where(predicate)));
        foreach (var availableNode in availableNodes)
        {
            _graphRepository.Setup(x => x.GetNodeAsync(availableNode.Id))
                .ReturnsAsync(availableNodes.First(n => n.Id == availableNode.Id));
        }

        NodeStateRepository.Setup(x => x.FilterFreeUnlockedNodes(availableNodes)).Returns(availableNodes);
        return this;
    }

    public ChargingPointManagerTestFixture WithLowestCostNode(TransportOrder.Domain.Models.Agv.Agv agv, IList<GraphNode> availableNodes, GraphNode node)
    {
        RouteService.Setup(x => x.GetNodeWithLowestCostsAsync(agv, availableNodes, It.IsAny<AgvData>())).ReturnsAsync(node);
        return this;
    }
}