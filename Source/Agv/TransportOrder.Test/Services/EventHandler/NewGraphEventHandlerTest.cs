using Gebhardt.Acc.Common.Messages.NewGraph;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class NewGraphEventHandlerTest
{
    [TestMethod]
    public async Task Consume_NewGraphReceived_PublishesAddNewGraphWithSameCorrelationId()
    {
        var fixture = new NewGraphEventHandlerTestFixture();

        var addNewGraph = await fixture.PublishAndGetFirstMessageAsync<NewGraphSegmentReceived, AddNewGraphSegment>(fixture.NewGraphSegmentReceived);

        addNewGraph.CorrelationId.ShouldBe(fixture.NewGraphSegmentReceived.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_NewGraphReceived_ForwardsTheCompleteGraph()
    {
        var fixture = new NewGraphEventHandlerTestFixture();
        var receivedGraph = fixture.NewGraphSegmentReceived.GraphSegment;

        var addNewGraph = await fixture.PublishAndGetFirstMessageAsync<NewGraphSegmentReceived, AddNewGraphSegment>(fixture.NewGraphSegmentReceived);

        addNewGraph.GraphSegment.Nodes.Select(n => n.Id).ShouldBe(receivedGraph.Nodes.Select(n => n.Id), true);
        addNewGraph.GraphSegment.Edges.Select(e => e.Id).ShouldBe(receivedGraph.Edges.Select(e => e.Id), true);
        addNewGraph.GraphSegment.Stations.Select(s => s.Id).ShouldBe(receivedGraph.Stations.Select(s => s.Id), true);
        addNewGraph.GraphSegment.Stations.Single().InteractionNodeIds.ShouldBe(receivedGraph.Stations.Single().InteractionNodeIds, true);
    }
}