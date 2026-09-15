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
            NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.StartNodeId),
            NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.EndNodeId)
        ], true);
        newGraphReceived.GraphSegment.Edges.ShouldHaveSingleItem().Id.ShouldBe(NewGraphGenerator.GenerateInternalEdgeId(NewGraphGenerator.EdgeId));
        newGraphReceived.GraphSegment.Stations.ShouldHaveSingleItem().Id.ShouldBe(NewGraphGenerator.GenerateInternalStationId(NewGraphGenerator.StationId));
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