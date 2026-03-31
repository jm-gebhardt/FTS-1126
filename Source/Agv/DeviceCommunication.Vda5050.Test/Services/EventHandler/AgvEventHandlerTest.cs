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
}