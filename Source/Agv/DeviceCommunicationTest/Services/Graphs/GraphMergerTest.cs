using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Graphs;

[TestClass]
public class GraphMergerTest
{
    [TestMethod]
    public async Task Consume_EmptyGraphWithAgvGraphReceived_MergesAndPublishesGraphChanged()
    {
        UpdateGraphRequest? sentRequest = null;
        var graph = new Graph();
        var fixture = new GraphMergerTestFixture()
            .WithCurrentGraph(graph)
            .WithSuccessfulUpdateGraphResponse(request =>
            {
                sentRequest = request;
                return new Success();
            })
            .Build();

        await fixture.PublishMessageAsync(fixture.InternalAgvGraphReceived);

        sentRequest.ShouldNotBeNull();
        sentRequest.CorrelationId.ShouldBe(fixture.InternalAgvGraphReceived.CorrelationId);

        var agvGraphReceived = await fixture.GetFirstPublishedMessageAsync<AgvGraphReceived>();
        agvGraphReceived.ShouldSatisfyAllConditions(
            () => agvGraphReceived.CorrelationId.ShouldBe(fixture.InternalAgvGraphReceived.CorrelationId),
            () => agvGraphReceived.SerialNumber.ShouldBe(fixture.InternalAgvGraphReceived.SerialNumber),
            () => agvGraphReceived.Graph.Nodes.Count.ShouldBe(fixture.InternalAgvGraphReceived.Graph.Nodes.Count)
        );
    }

    [TestMethod]
    public async Task Consume_ExistingGraphWithKnownAgvGraphReceived_DoesNotPublishesGraphChanged()
    {
        UpdateGraphRequest? sentRequest = null;
        var fixture = new GraphMergerTestFixture();
        fixture.WithCurrentGraph(fixture.InternalAgvGraphReceived.Graph.ToGraph())
            .WithSuccessfulUpdateGraphResponse(request =>
            {
                sentRequest = request;
                return new Success();
            })
            .Build();

        await fixture.PublishMessageAsync(fixture.InternalAgvGraphReceived);
        sentRequest.ShouldBeNull();
    }

    // test that AgvGraphReceived
}