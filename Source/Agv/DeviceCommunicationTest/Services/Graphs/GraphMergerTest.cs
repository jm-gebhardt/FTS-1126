using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Graphs;

[TestClass]
public class GraphMergerTest
{
    [TestMethod]
    public async Task Consume_EmptyGraphWithAgvGraphReceived_MergesAndPublishesGraphChanged()
    {
        var fixture = new GraphMergerTestFixture()
            .WithCurrentGraph(new Graph());

        var graphChanged = await fixture.PublishAndGetFirstMessageAsync<AgvGraphReceived, GraphChanged>(
            fixture.AgvGraphReceived);

        graphChanged.CorrelationId.ShouldBe(fixture.AgvGraphReceived.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_ExistingGraphWithKnownAgvGraphReceived_DoesNotPublishesGraphChanged()
    {
        var fixture = new GraphMergerTestFixture();
        fixture.WithCurrentGraph(fixture.AgvGraphReceived.Graph.ToGraph());

        await fixture.PublishMessageAsync(fixture.AgvGraphReceived);

        await fixture.ShouldNotHavePublishedAsync<GraphChanged>();
    }
}