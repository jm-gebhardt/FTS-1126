using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class GraphEventHandlerTest
{
    [TestMethod]
    public async Task Consume_GraphChanged_PublishesUpdateGraph()
    {
        var fixture = new GraphEventHandlerTestFixture();
        var graphChanged = new GraphChanged(Guid.NewGuid(), GraphGenerator.CreateDefaultGraph(), new List<Map>());

        await fixture.PublishMessageAsync(graphChanged);

        var updateGraph = await fixture.GetFirstPublishedMessageAsync<UpdateGraph>();

        updateGraph.CorrelationId.ShouldBe(graphChanged.CorrelationId);
        updateGraph.Graph.ShouldBeEquivalentTo(graphChanged.Graph);
        updateGraph.Maps.ShouldBeEquivalentTo(graphChanged.Maps);
    }
}