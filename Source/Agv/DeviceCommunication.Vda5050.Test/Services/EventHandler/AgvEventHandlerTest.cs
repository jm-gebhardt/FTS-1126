using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

[TestClass]
public class AgvEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvAddedOrUpdated_PublishesPublishAgvGraph()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), serialNumber: "agv1");
        var fixture = new AgvEventHandlerTestFixture()
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new AgvAddedOrUpdated(agv.CorrelationId, agv.SerialNumber));

        var published = await fixture.GetFirstPublishedMessageAsync<PublishAgvGraph>();
        published.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        published.SerialNumber.ShouldBe(agv.SerialNumber);
        published.VehicleTypeId.ShouldBe(agv.VehicleTypeId);
    }

    [TestMethod]
    public async Task Consume_AgvDiscovered_PublishesAddAgv()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), serialNumber: "agv1");
        var agvTypeId = Guid.NewGuid();
        var fixture = new AgvEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new AgvDiscovered(agv.CorrelationId, agv, agvTypeId, false));

        var published = await fixture.GetFirstPublishedMessageAsync<AddAgv>();
        published.CorrelationId.ShouldBe(agv.CorrelationId);
        published.Agv.CorrelationId.ShouldBe(agv.CorrelationId);
        published.AgvTypeId.ShouldBe(agvTypeId);
        published.HasValidConfiguration.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvTypeChanged_PublishesUpdateAgvType()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), serialNumber: "agv1");
        var newTypeId = Guid.NewGuid();
        var fixture = new AgvEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new AgvTypeChanged(agv.CorrelationId, newTypeId, agv, false, true));

        var published = await fixture.GetFirstPublishedMessageAsync<UpdateAgvType>();
        published.CorrelationId.ShouldBe(agv.CorrelationId);
        published.NewTypeId.ShouldBe(newTypeId);
        published.Agv.CorrelationId.ShouldBe(agv.CorrelationId);
        published.HasValidConfiguration.ShouldBeFalse();
        published.NeedsConfirmation.ShouldBeTrue();
    }
}