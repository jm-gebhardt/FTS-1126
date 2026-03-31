using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Shouldly;
using DomainAgvGraph = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.AgvGraph;
using CommonAgvGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class PublishAgvGraphHandlerTest
{
    [TestMethod]
    public async Task Consume_LayoutExists_PublishesInternalAgvGraphReceived()
    {
        var agvId = Guid.NewGuid();
        var domainGraph = new DomainAgvGraph(agvId,
            [new Node { Id = Guid.NewGuid(), NodeId = "n1", MapId = "map", Position = new NodePosition(1, 1), AgvActions = [] }],
            []);
        var commonGraph = new CommonAgvGraph(agvId,
            [new GraphNode(Guid.NewGuid(), "n1") { X = 1, Y = 1 }],
            []);

        var fixture = new PublishAgvGraphHandlerTestFixture()
            .SetDomainGraph(agvId, domainGraph, commonGraph);

        await fixture.TestHarness.Bus.Publish(new PublishAgvGraph(agvId, "agv1", "Maker.S1"));
        await fixture.ShouldHaveConsumedAsync<PublishAgvGraph>();

        var published = await fixture.GetFirstPublishedMessageAsync<InternalAgvGraphReceived>();
        published.CorrelationId.ShouldBe(agvId);
    }

    [TestMethod]
    public async Task Consume_LayoutExists_PublishesAgvMapReceived()
    {
        var agvId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var domainGraph = new DomainAgvGraph(agvId,
            [new Node { Id = nodeId, NodeId = "n1", MapId = "map", Position = new NodePosition(1, 1), AgvActions = [] }],
            []);
        var commonGraph = new CommonAgvGraph(agvId,
            [new GraphNode(nodeId, "n1") { X = 1, Y = 1 }],
            []);

        var fixture = new PublishAgvGraphHandlerTestFixture()
            .SetDomainGraph(agvId, domainGraph, commonGraph);

        await fixture.TestHarness.Bus.Publish(new PublishAgvGraph(agvId, "agv1", "Maker.S1"));
        await fixture.ShouldHaveConsumedAsync<PublishAgvGraph>();

        await fixture.ShouldHavePublishedAsync<AgvMapReceived>();
    }

    [TestMethod]
    public async Task Consume_NoLayout_DoesNotPublish()
    {
        var agvId = Guid.NewGuid();
        var fixture = new PublishAgvGraphHandlerTestFixture()
            .SetNoDomainGraph(agvId);

        await fixture.TestHarness.Bus.Publish(new PublishAgvGraph(agvId, "agv1", "Maker.S1"));
        await fixture.ShouldHaveConsumedAsync<PublishAgvGraph>();

        await fixture.ShouldNotHavePublishedAsync<InternalAgvGraphReceived>();
        await fixture.ShouldNotHavePublishedAsync<AgvMapReceived>();
    }
}