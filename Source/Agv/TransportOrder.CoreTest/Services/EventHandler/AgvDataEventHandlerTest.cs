using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

[TestClass]
public class AgvDataEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvDataReceived_ValidData_PublishesUpdateAgvData()
    {
        var fixture = new MassTransitConsumerTestFixture<AgvDataEventHandler>();
        var agvData = new AgvData(Guid.NewGuid(), "model", "type", string.Empty, 100d, 1.0, 10d, 10d, 1.0, 10d, 1.0, 1.0, 42, Array.Empty<AgvData.Sensor>());

        await fixture.TestHarness.Bus.Publish(new AgvDataReceived(agvData.CorrelationId, "agvId", agvData));
        (await fixture.ConsumerHarness.Consumed.Any<AgvDataReceived>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<UpdateAgvData>()).ShouldBeTrue();
    }
}