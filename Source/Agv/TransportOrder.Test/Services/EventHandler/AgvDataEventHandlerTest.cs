using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

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