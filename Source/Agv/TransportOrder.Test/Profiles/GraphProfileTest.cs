using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Graph = Gebhardt.Acc.Common.Domain.Models.Graphs.Graph;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode;

namespace Gebhardt.Acc.TransportOrder.Test.Profiles;

[TestClass]
public class GraphProfileTest
{
    [TestMethod]
    public void Map_GraphNodeWithAdditionalType_MapsNodeTypeCorrectly()
    {
        var graphNode = new GraphNode("nodeId", 1, 1, 0, [AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park]);

        var mapper = MappingHelper.GetMapper(new AgvGraphProfile());

        var graphNodeMessageContent = mapper.Map<TransportOrder.Messages.MessageContent.GraphNode>(graphNode);

        graphNodeMessageContent.Type.ShouldBe(graphNode.GetPrimaryType());
    }

    [TestMethod]
    public void Map_Graph_NodeWithAdditionalType_MapsAllProperties()
    {
        IGraph agvGraph = AgvGraphGenerator.CreateGraphWithAdditionalStationType();

        IGraph graph = new Graph() { Nodes = agvGraph.Nodes, Edges = agvGraph.Edges };
        var mapper = MappingHelper.GetMapper(new GraphProfile());

        var graphMessageContent = mapper.Map<TransportOrder.Messages.MessageContent.Graph>(graph);

        graphMessageContent.ShouldNotBeNull();
        graphMessageContent.Nodes.Single(n => n.Id == AgvGraphGenerator.Double1.Id).Type.ShouldBe(graph.Nodes.Single(n => n.Id == AgvGraphGenerator.Double1.Id).GetPrimaryType());
    }
}