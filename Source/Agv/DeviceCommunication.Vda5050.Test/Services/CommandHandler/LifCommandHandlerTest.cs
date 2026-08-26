using Gebhardt.Acc.Common.Messages.NewGraph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class LifCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StoreLif_PublishesNewGraphReceivedForTheStoredLayouts()
    {
        var fixture = new LifCommandHandlerTestFixture();

        var newGraphReceived = await fixture.PublishAndGetFirstMessageAsync<StoreLif, NewGraphReceived>(fixture.StoreLif);

        newGraphReceived.CorrelationId.ShouldBe(fixture.StoreLif.CorrelationId);
        newGraphReceived.Graph.Nodes.Select(n => n.Id).ShouldBe(
        [
            NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.StartNodeId),
            NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.EndNodeId)
        ], true);
        newGraphReceived.Graph.Edges.ShouldHaveSingleItem().Id.ShouldBe(NewGraphGenerator.GenerateInternalEdgeId(NewGraphGenerator.EdgeId));
        newGraphReceived.Graph.Stations.ShouldHaveSingleItem().Id.ShouldBe(NewGraphGenerator.GenerateInternalStationId(NewGraphGenerator.StationId));
    }

    [TestMethod]
    public async Task Consume_StoreLifOlderThanTheStoredLif_DoesNotPublishNewGraphReceived()
    {
        var fixture = new LifCommandHandlerTestFixture().WithNewerStoredLif(NewGraphGenerator.VehicleTypeId);

        await fixture.PublishMessageAsync(fixture.StoreLif);

        await fixture.ShouldHavePublishedAsync<LayoutStored>();
        await fixture.ShouldNotHavePublishedAsync<NewGraphReceived>();
    }

    [TestMethod]
    public async Task Consume_StoringTheLifFails_DoesNotPublishNewGraphReceived()
    {
        var fixture = new LifCommandHandlerTestFixture().WithFailingStore();

        await fixture.PublishMessageAsync(fixture.StoreLif);

        await fixture.ShouldHavePublishedAsync<LifStoringFailed>();
        await fixture.ShouldNotHavePublishedAsync<NewGraphReceived>();
    }

    [TestMethod]
    public async Task Consume_StoreLifPartiallyTakenOver_PublishesOnlyTheTakenOverLayouts()
    {
        var fixture = new LifCommandHandlerTestFixture()
            .WithLayouts(
                NewGraphGenerator.GenerateLayout(),
                NewGraphGenerator.GenerateLayout(NewGraphGenerator.OtherLayoutId, vehicleTypeId: NewGraphGenerator.OtherVehicleTypeId))
            .WithNewerStoredLif(NewGraphGenerator.VehicleTypeId);

        var newGraphReceived = await fixture.PublishAndGetFirstMessageAsync<StoreLif, NewGraphReceived>(fixture.StoreLif);

        newGraphReceived.Graph.Nodes.Select(n => n.Id).ShouldBe(
        [
            NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.StartNodeId),
            NewGraphGenerator.GenerateInternalNodeId(NewGraphGenerator.EndNodeId)
        ], true);
    }
}