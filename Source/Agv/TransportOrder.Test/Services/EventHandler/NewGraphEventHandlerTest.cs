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

        var addNewGraph = await fixture.PublishAndGetFirstMessageAsync<NewGraphReceived, AddNewGraph>(fixture.NewGraphReceived);

        addNewGraph.CorrelationId.ShouldBe(fixture.NewGraphReceived.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_NewGraphReceived_ForwardsTheCompleteGraph()
    {
        var fixture = new NewGraphEventHandlerTestFixture();
        var receivedGraph = fixture.NewGraphReceived.Graph;

        var addNewGraph = await fixture.PublishAndGetFirstMessageAsync<NewGraphReceived, AddNewGraph>(fixture.NewGraphReceived);

        addNewGraph.Graph.Nodes.Select(n => n.Id).ShouldBe(receivedGraph.Nodes.Select(n => n.Id), true);
        addNewGraph.Graph.Edges.Select(e => e.Id).ShouldBe(receivedGraph.Edges.Select(e => e.Id), true);
        addNewGraph.Graph.Stations.Select(s => s.Id).ShouldBe(receivedGraph.Stations.Select(s => s.Id), true);
        addNewGraph.Graph.Stations.Single().InteractionNodes.Select(n => n.Id)
            .ShouldBe(receivedGraph.Stations.Single().InteractionNodes.Select(n => n.Id), true);
    }
}