using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Moq;
using Shouldly;
using DomainAgvGraph = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.AgvGraph;
using CommonAgvGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class LifGraphConverterTest
{
    [TestMethod]
    public void ConvertToCommonAgvGraph_MapsNodes()
    {
        var fixture = new LifGraphConverterTestFixture();
        var agvId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var domainGraph = new DomainAgvGraph(agvId,
            [new Node { Id = nodeId, NodeId = "n1", MapId = "map", Position = new NodePosition(5.0, 10.0), AgvActions = [] }],
            []);

        var result = fixture.Converter.ConvertToCommonAgvGraph(domainGraph);

        result.AgvCorrelationId.ShouldBe(agvId);
        result.Nodes.Count.ShouldBe(1);
        result.Nodes.Single().Id.ShouldBe(nodeId);
        result.Nodes.Single().X.ShouldBe(5.0f);
        result.Nodes.Single().Y.ShouldBe(10.0f);
    }

    [TestMethod]
    public void ConvertToCommonAgvGraph_AppliesStationNodeTypes()
    {
        var fixture = new LifGraphConverterTestFixture();
        var agvId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var domainGraph = new DomainAgvGraph(agvId,
            [
                new Node
                {
                    Id = nodeId,
                    NodeId = "n1",
                    MapId = "map",
                    Position = new NodePosition(1, 1),
                    AgvActions = [new AgvNodeAction { ActionType = "pick" }]
                }
            ],
            []);

        var result = fixture.Converter.ConvertToCommonAgvGraph(domainGraph);

        result.Nodes.Single().Type.ShouldBe(CommonAgvGraph.NodeType.Pick);
    }

    [TestMethod]
    public void ConvertToCommonAgvGraph_BuildsEdgesFromNurbs()
    {
        var fixture = new LifGraphConverterTestFixture();
        var agvId = Guid.NewGuid();
        var node1Id = Guid.NewGuid();
        var node2Id = Guid.NewGuid();
        var edgeId = Guid.NewGuid();
        var expectedEdge = new GraphEdge { Id = edgeId, FromNodeId = node1Id, ToNodeId = node2Id };

        var trajectory = new Trajectory
        {
            Degree = 1,
            KnotVector = [0f, 0f, 1f, 1f],
            ControlPoints =
            [
                new ControlPoint { X = 0, Y = 0, Weight = 1 },
                new ControlPoint { X = 1, Y = 1, Weight = 1 }
            ]
        };

        fixture.NurbsEdgeBuilder
            .Setup(x => x.BuildFromNurbs(It.IsAny<Nurbs>(), node1Id, node2Id, edgeId, It.IsAny<DateTime>()))
            .Returns(expectedEdge);

        var domainGraph = new DomainAgvGraph(agvId,
            [
                new Node { Id = node1Id, NodeId = node1Id.ToString(), MapId = "map", Position = new NodePosition(0, 0), AgvActions = [] },
                new Node { Id = node2Id, NodeId = node2Id.ToString(), MapId = "map", Position = new NodePosition(1, 1), AgvActions = [] }
            ],
            [
                new Edge
                {
                    Id = edgeId,
                    EdgeId = "e1",
                    StartNodeId = node1Id.ToString(),
                    EndNodeId = node2Id.ToString(),
                    AgvTrajectory = trajectory,
                    AgvActions = []
                }
            ]);

        var result = fixture.Converter.ConvertToCommonAgvGraph(domainGraph);

        result.Edges.Count.ShouldBe(1);
        result.Edges.Single().Id.ShouldBe(edgeId);
    }

    [TestMethod]
    public void ConvertToCommonAgvGraph_EdgeWithoutTrajectory_Throws()
    {
        var fixture = new LifGraphConverterTestFixture();
        var agvId = Guid.NewGuid();
        var node1Id = Guid.NewGuid();
        var node2Id = Guid.NewGuid();

        var domainGraph = new DomainAgvGraph(agvId,
            [
                new Node { Id = node1Id, NodeId = node1Id.ToString(), MapId = "map", Position = new NodePosition(0, 0), AgvActions = [] },
                new Node { Id = node2Id, NodeId = node2Id.ToString(), MapId = "map", Position = new NodePosition(1, 1), AgvActions = [] }
            ],
            [
                new Edge
                {
                    Id = Guid.NewGuid(),
                    EdgeId = "e1",
                    StartNodeId = node1Id.ToString(),
                    EndNodeId = node2Id.ToString(),
                    AgvTrajectory = new Trajectory(),
                    AgvActions = []
                }
            ]);

        Should.Throw<InvalidOperationException>(() => fixture.Converter.ConvertToCommonAgvGraph(domainGraph));
    }
}