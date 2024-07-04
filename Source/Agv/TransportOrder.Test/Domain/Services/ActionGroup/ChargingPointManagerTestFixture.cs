using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
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
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

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

    public AgvGraph AgvGraph { get; private set; } = null!;
    
    public TransportOrder.Domain.Models.Agv.Agv Agv { get; private set; } = null!;
    
    public IChargingPointManager ChargingPointManager { get; private set; } = null!;
    
    private readonly Mock<IAgvDb> _agvDb = new();

    public Mock<INodeStateRepository> NodeStateRepository { get; } = new();

    public Mock<IRouteService> RouteService { get; } = new();

    private readonly GraphNode[] _nodes =
    {
        NodeGenerator.GenerateNode(AgvGraph.NodeType.Charge,-2f, -2f, nodeId:_charge),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop,0f, 0f, nodeId:_node01),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop,4f, 0f, nodeId:_node02),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop,0f, 3f, nodeId:_node03),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop,4f, 3f, nodeId:_node04),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop,6f, 9f, nodeId:_node05),
        NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop,13f, 5f, nodeId:_node06)
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

    public ChargingPointManagerTestFixture WithGraphRepository(bool withChargerNode)
    {
        var correlationId = AgvSerialNumber.ToGuid();
        
        GraphNode[] nodes = _nodes;
        GraphEdge[] edges = _edges;
        if (withChargerNode)
        {
            AgvGraph = new AgvGraph(correlationId, nodes, edges);
        }
        else
        {
            nodes = nodes.Where(n => n.Type != AgvGraph.NodeType.Charge).ToArray();
            edges = edges.Where(edge => nodes.FirstOrDefault(node => node.Id == edge.FromNodeId)?.Type != AgvGraph.NodeType.Charge
                                        || nodes.FirstOrDefault(node => node.Id == edge.ToNodeId)?.Type != AgvGraph.NodeType.Charge).ToArray();

            AgvGraph = new AgvGraph(correlationId, nodes, edges);
        }

        SetupGraphRepository(AgvGraph);
        return this;
    }
    
    public ChargingPointManagerTestFixture WithNodeLocker(bool reserveSuccess, bool lockSuccess, bool unlockSuccess, Func<IQueryable<GraphNode>, IQueryable<GraphNode>>? filterFreeUnlockedNodesFunction = null)
    {
        SetupNodeLocker(reserveSuccess, lockSuccess, unlockSuccess, filterFreeUnlockedNodesFunction);
        return this;
    }
    
    public ChargingPointManagerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv? agv = null)
    {
        if (agv == null)
        {
            var correlationId = AgvSerialNumber.ToGuid();
            var agvStatus = new AgvStatus(correlationId, "Idle", true, true, 10, false, false, "");

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
        ChargingPointManager = new ChargingPointManager(_agvDb.Object, NodeStateRepository.Object, RouteService.Object, new Mock<ILogger<ChargingPointManager>>().Object, new SemaphoreProvider<IChargingSemaphore>());
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

        NodeStateRepository.Setup(x => x.FilterFreeUnlockedNodes(availableNodes)).Returns(availableNodes);
        return this;
    }

    public ChargingPointManagerTestFixture WithLowestCostNode(IList<GraphNode> availableNodes, GraphNode node)
    {
        RouteService.WithRouteLowestCostNode(Agv, availableNodes, node);
        return this;
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
        _agvDb.AddAgvGraph(graph);
        _agvDb.Setup(x => x.Graphs.GetNodesAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<GraphNode, bool>>>()))
            .ReturnsAsync(new Func<Guid, Expression<Func<GraphNode, bool>>, IEnumerable<GraphNode>>((_, predicate) => graph.Nodes.AsQueryable().Where(predicate)));
    }
}