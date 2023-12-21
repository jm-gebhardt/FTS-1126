using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Graphs;

[TestClass]
public class GraphMergerTest
{
    [TestMethod]
    public async Task Consume_EmptyGraphWithAgvGraphReceived_MergesAndPublishesGraphChanged()
    {
        var fixture = new GraphMergerTestFixture()
            .WithCurrentGraph(new Graph());

        await fixture.PublishMessageAsync(fixture.AgvGraphReceived);

        var graphChanged = await fixture.GetFirstPublishedMessageAsync<GraphChanged>();

        graphChanged.CorrelationId.ShouldBe(fixture.AgvGraphReceived.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_ExistingGraphWithKnownAgvGraphReceived_DoesNotPublishesGraphChanged()
    {
        var fixture = new GraphMergerTestFixture();
        fixture.WithCurrentGraph(fixture.AgvGraphReceived.Graph.ToGraph());

        await fixture.PublishMessageAsync(fixture.AgvGraphReceived);

        (await fixture.TestHarness.Published.Any<GraphChanged>()).ShouldBeFalse();
    }
}