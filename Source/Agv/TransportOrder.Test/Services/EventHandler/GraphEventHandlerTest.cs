using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class GraphEventHandlerTest
{
    [TestMethod]
    public async Task Consume_GraphChanged_PublishesUpdateGraph()
    {
        var fixture = new GraphEventHandlerTestFixture();
        var graphChanged = new GraphChanged(Guid.NewGuid(), GraphGenerator.CreateDefaultGraph(), new List<Map>());

        var updateGraph = await fixture.PublishAndGetFirstMessageAsync<GraphChanged, UpdateGraph>(
            graphChanged);

        updateGraph.CorrelationId.ShouldBe(graphChanged.CorrelationId);
        updateGraph.Graph.ShouldBeEquivalentTo(graphChanged.Graph);
        updateGraph.Maps.ShouldBeEquivalentTo(graphChanged.Maps);
    }
}