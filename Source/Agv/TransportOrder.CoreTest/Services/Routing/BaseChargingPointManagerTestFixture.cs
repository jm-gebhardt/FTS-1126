using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Moq;

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

    private readonly AgvGraph.Node[] _nodes = new[]
    {
        new AgvGraph.Node(_charge){ Enabled = true, Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 0f},
        new AgvGraph.Node(_node01){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f},
        new AgvGraph.Node(_node02){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f},
        new AgvGraph.Node(_node03){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 3f, Z = 0f},
        new AgvGraph.Node(_node04){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 3f, Z = 0f},
        new AgvGraph.Node(_node05){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 9f, Z = 0f},
        new AgvGraph.Node(_node06){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 13f, Y = 5f, Z = 0f}
    };

    private readonly AgvGraph.Edge[] _edges = new[]
    {
        new AgvGraph.Edge(_charge, _node01){ Metric = 2f},
        new AgvGraph.Edge(_node01, _charge){ Metric = 2f},
        new AgvGraph.Edge(_node01, _node02){ Metric = 4f},
        new AgvGraph.Edge(_node01, _node03){ Metric = 3f},
        new AgvGraph.Edge(_node01, _node04){ Metric = 5f},
        new AgvGraph.Edge(_node02, _node01){ Metric = 4f},
        new AgvGraph.Edge(_node02, _node04){ Metric = 6f},
        new AgvGraph.Edge(_node03, _node01){ Metric = 3f},
        new AgvGraph.Edge(_node03, _node04){ Metric = 4f},
        new AgvGraph.Edge(_node04, _node01){ Metric = 5f},
        new AgvGraph.Edge(_node04, _node02){ Metric = 6f},
        new AgvGraph.Edge(_node04, _node03){ Metric = 4f}
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

    public IChargingPointManager GetBaseChargingPointManager()
    {
        return new BaseChargingPointManager(GetGraphRegistry().Object);
    }
    
    private Mock<IGraphRegistry> GetGraphRegistry()
    {
        var graphRegistryMock = new Mock<IGraphRegistry>();
        var sequence = new MockSequence();
        graphRegistryMock.InSequence(sequence)
            .Setup(s => s.ReserveNode(It.IsAny<string>()))
            .Callback((string nodeId) =>
            {
                _nodes.FirstOrDefault(node => node.Id == nodeId)!.State = AgvGraph.NodeState.Reserved;
            })
            .Returns(true);

        return graphRegistryMock;
    }
}
