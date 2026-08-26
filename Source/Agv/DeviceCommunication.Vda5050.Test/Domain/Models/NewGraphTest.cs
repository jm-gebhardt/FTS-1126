using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Models;

[TestClass]
public class NewGraphTest
{
    [TestMethod]
    public void ConvertToGraph_ValidData_CorrectlyConverted()
    {
        var layoutCollection = NewGraphGenerator.SampleCollection;

        var graph = layoutCollection.ToGraph(layoutCollection.Layouts);

        graph.Edges.Count.ShouldBe(1);
        graph.Nodes.Count.ShouldBe(2);
        graph.Stations.Count.ShouldBe(1);
    }

    [TestMethod]
    public void ConvertToGraph_SubsetOfLayouts_OnlyConvertsTheGivenLayouts()
    {
        var layout = NewGraphGenerator.GenerateLayout();
        var layoutCollection = NewGraphGenerator.GenerateCollection(layout, NewGraphGenerator.GenerateLayout(NewGraphGenerator.OtherLayoutId));

        var graph = layoutCollection.ToGraph([layout]);

        graph.Nodes.Select(n => n.Id).ShouldBe(
        [
            NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.StartNodeId),
            NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.EndNodeId)
        ], ignoreOrder: true);
    }

    [TestMethod]
    public void ConvertToGraph_ValidData_EdgeAndStationReferenceTheNodesOfTheLayout()
    {
        var layoutCollection = NewGraphGenerator.GenerateCollection(NewGraphGenerator.GenerateLayout());

        var graph = layoutCollection.ToGraph(layoutCollection.Layouts);

        var startNodeId = NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.StartNodeId);
        var endNodeId = NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.EndNodeId);
        var edge = graph.Edges.ShouldHaveSingleItem();
        edge.Id.ShouldBe(NewGraphGenerator.GenerateInternalEdgeId(NewGraphGenerator.EdgeId));
        edge.StartNodeId.ShouldBe(startNodeId);
        edge.EndNodeId.ShouldBe(endNodeId);
        var station = graph.Stations.ShouldHaveSingleItem();
        station.Id.ShouldBe(NewGraphGenerator.GenerateInternalStationId(NewGraphGenerator.StationId));
        station.InteractionNodeIds.ShouldHaveSingleItem().ShouldBe(startNodeId);
    }

    [TestMethod]
    public void ConvertToGraph_ValidData_TakesNodeLevelIdFromLayout()
    {
        var layoutCollection = NewGraphGenerator.GenerateCollection(NewGraphGenerator.GenerateLayout());

        var graph = layoutCollection.ToGraph(layoutCollection.Layouts);

        graph.Nodes.ShouldAllBe(n => n.LevelId == NewGraphGenerator.LevelId);
    }
}