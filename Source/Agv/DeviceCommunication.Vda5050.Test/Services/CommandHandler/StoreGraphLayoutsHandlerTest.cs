using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class StoreGraphLayoutsHandlerTest
{
    [TestMethod]
    public async Task Consume_StoresLayoutsAndPublishesGraphLayoutStored()
    {
        var fixture = new StoreGraphLayoutsHandlerTestFixture();
        var layout = GraphLayoutGenerator.GenerateLayout(
            nodes: [GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: "Maker.S1")]);
        var message = new StoreGraphLayouts([layout]);

        await fixture.TestHarness.Bus.Publish(message);
        await fixture.ShouldHaveConsumedAsync<StoreGraphLayouts>();

        fixture.GraphLayoutRepository.Verify(
            r => r.StoreGraphLayoutsAsync(It.Is<ICollection<GraphLayout>>(c => c.Count == 1)),
            Times.Once);

        var published = await fixture.GetFirstPublishedMessageAsync<GraphLayoutStored>();
        published.LayoutIds.ShouldContain("layout-1");
        published.VehicleTypeIds.ShouldContain("Maker.S1");
    }

    [TestMethod]
    public async Task Consume_MultipleLayouts_PublishesAllLayoutIdsAndVehicleTypes()
    {
        var fixture = new StoreGraphLayoutsHandlerTestFixture();
        var layout1 = GraphLayoutGenerator.GenerateLayout("L1",
            nodes: [GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: "Maker.A")]);
        var layout2 = GraphLayoutGenerator.GenerateLayout("L2",
            nodes: [GraphLayoutGenerator.GenerateNode("n2", vehicleTypeIds: "Maker.B")]);
        var message = new StoreGraphLayouts([layout1, layout2]);

        await fixture.TestHarness.Bus.Publish(message);
        await fixture.ShouldHaveConsumedAsync<StoreGraphLayouts>();

        var published = await fixture.GetFirstPublishedMessageAsync<GraphLayoutStored>();
        published.LayoutIds.Count.ShouldBe(2);
        published.VehicleTypeIds.Count.ShouldBe(2);
    }
}