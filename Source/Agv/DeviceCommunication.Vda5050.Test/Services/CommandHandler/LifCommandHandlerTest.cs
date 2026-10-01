using Gebhardt.Acc.Common.Messages.NewGraph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class LifCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StoreLif_PublishesNewGraphSegmentReceivedForTheStoredLayouts()
    {
        var fixture = new LifCommandHandlerTestFixture();

        var newGraphReceived = await fixture.PublishAndGetFirstMessageAsync<StoreLif, NewGraphSegmentReceived>(fixture.StoreLif);

        newGraphReceived.CorrelationId.ShouldBe(fixture.StoreLif.CorrelationId);
        newGraphReceived.GraphSegment.Nodes.Select(n => n.Id).ShouldBe(
        [
            DomainLifGenerator.GenerateInternalNodeId(DomainLifGenerator.StartNodeId),
            DomainLifGenerator.GenerateInternalNodeId(DomainLifGenerator.EndNodeId)
        ], true);
        newGraphReceived.GraphSegment.Edges.ShouldHaveSingleItem().Id.ShouldBe(DomainLifGenerator.GenerateInternalEdgeId(DomainLifGenerator.EdgeId));
        newGraphReceived.GraphSegment.Stations.ShouldHaveSingleItem().Id.ShouldBe(DomainLifGenerator.GenerateInternalStationId(DomainLifGenerator.StationId));
    }

    [TestMethod]
    public async Task Consume_StoringTheLifFails_DoesNotPublishNewGraphSegmentReceived()
    {
        var fixture = new LifCommandHandlerTestFixture().WithFailingStore();

        await fixture.PublishMessageAsync(fixture.StoreLif);

        await fixture.ShouldHavePublishedAsync<LifStoringFailed>();
        await fixture.ShouldNotHavePublishedAsync<NewGraphSegmentReceived>();
    }
}