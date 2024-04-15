using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class AgvDataEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvDataReceived_ValidData_PublishesUpdateAgvData()
    {
        var fixture = new AgvDataEventHandlerTestFixture();
        var agvData = AgvDataGenerator.GenerateAgvData();

        await fixture.TestHarness.Bus.Publish(new AgvDataReceived(agvData.CorrelationId, "agvId", agvData));
        (await fixture.ConsumerHarness.Consumed.Any<AgvDataReceived>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<UpdateAgvData>()).ShouldBeTrue();
    }
}