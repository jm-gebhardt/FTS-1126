using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Moq;

// ReSharper disable MemberCanBePrivate.Global

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing;

public class BaseChargingPointManagerTestFixture
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
    private const string MapName = "Map#1";

    public readonly Mock<IGraphRegistry> GraphRegistry = new();
    public readonly Mock<INodeStateRegistry> NodeStateRegistry = new();

    private readonly AgvGraph.Node[] _nodes = {
        new(_charge){ Enabled = true, Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 0f},
        new(_node01){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f},
        new(_node02){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f},
        new(_node03){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 3f, Z = 0f},
        new(_node04){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 3f, Z = 0f},
        new(_node05){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 9f, Z = 0f},
        new(_node06){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 13f, Y = 5f, Z = 0f}
    };

    private readonly AgvGraph.Edge[] _edges =
    {
        new (_charge, _node01){ Metric = 2f},
        new (_node01, _charge){ Metric = 2f},
        new (_node01, _node02){ Metric = 4f},
        new (_node01, _node03){ Metric = 3f},
        new (_node01, _node04){ Metric = 5f},
        new (_node02, _node01){ Metric = 4f},
        new (_node02, _node04){ Metric = 6f},
        new (_node03, _node01){ Metric = 3f},
        new (_node03, _node04){ Metric = 4f},
        new (_node04, _node01){ Metric = 5f},
        new (_node04, _node02){ Metric = 6f},
        new (_node04, _node03){ Metric = 4f}
        //no edges for node 5 and 6 on purpose so that there is no route to this stations
    };

    public AgvGraph GetAgvGraph(bool withChargerNode, Guid? correlationId = null)
    {
        AgvGraph.Node[] nodes = _nodes;
        AgvGraph.Edge[] edges = _edges;
        if (withChargerNode)
        {
            return new AgvGraph(correlationId ?? Guid.NewGuid(), nodes, edges, MapName.ToGuid(), MapName);
        }
        
        nodes = nodes.Where(n => n.Type != AgvGraph.NodeType.Charge).ToArray();
        edges = edges.Where(edge => nodes.FirstOrDefault(node => node.Id == edge.From)?.Type != AgvGraph.NodeType.Charge
                                    || nodes.FirstOrDefault(node => node.Id == edge.To)?.Type != AgvGraph.NodeType.Charge).ToArray();
        return new AgvGraph(correlationId ?? Guid.NewGuid(), nodes, edges, MapName.ToGuid(), MapName);
    }

    public Graph GetGraphWhereNoOneUnderstandWhyWeHaveTwoDifferentGraphTypes(AgvGraph agvGraph)
    {
        var nodes = new List<Node>();
        foreach (var node in agvGraph.Nodes)
        {
            nodes.Add(new()
            {
                Id = node.Id,
                Type = node.Type,
                X = node.X,
                Y = node.Y,
                Z = node.Z,
                Theta = node.Theta
            });
        }

        var edges = new List<Edge>();
        foreach (var edge in agvGraph.Edges)
        {
            edges.Add(new()
            {
                FromId = edge.From,
                ToId = edge.To,
                Metric = edge.Metric,
                IsBlocked = edge.IsBlocked
            });
        }
        return new Graph(nodes.ToArray(), edges.ToArray());
    }

    public IChargingPointManager CreateBaseChargingPointManager()
    {
        return new ChargingPointManager(GraphRegistry.Object, NodeStateRegistry.Object);
    }
    
    public void SetupNodeLocker(bool reserveSuccess, bool lockSuccess, bool unlockSuccess, Func<IQueryable<Node>, IQueryable<Node>>? filterFreeUnlockedNodesFunction = null)
    {
        NodeStateRegistry.Setup(s => s.ReserveNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).Returns(Task.FromResult(reserveSuccess));
        NodeStateRegistry.Setup(s => s.LockNodeAsync(It.IsAny<string>())).Returns(Task.FromResult(lockSuccess));
        NodeStateRegistry.Setup(s => s.UnlockNodeAsync(It.IsAny<string>())).Returns(Task.FromResult(unlockSuccess));
        
        filterFreeUnlockedNodesFunction ??= nodes => nodes;
        NodeStateRegistry.Setup(s => s.FilterFreeUnlockedNodes(It.IsAny<IQueryable<Node>>())).Returns(filterFreeUnlockedNodesFunction);
    }
    
    public void SetupGraphRegistry(Graph graph)
    {
        GraphRegistry.Setup(x => x.GetGraph()).Returns(graph);
        GraphRegistry.Setup(x => x.GetNodes(It.IsAny<Expression<Func<Node, bool>>>(), It.IsAny<IEnumerable<IAgv>?>()))
            .Returns<Expression<Func<Node, bool>>, IEnumerable<IAgv>?>((f, _) => graph.Nodes.AsQueryable().Where(f));
    }

    public IAgv GetAgv(Guid correlationId, string agvId, string state, string order, string ip, AgvGraph agvGraph)
    {
        var agvStatus = new AgvStatus(correlationId, order, true, true, 10, false, false);
        var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());

        return new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph, agvData) { IsOnline = true };
    }
}
