using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Consumers;

/// <summary>
/// Test for finishing host orders
/// </summary>
[TestClass]
public class ConfirmHostOrderConsumerSimTest
{
    [TestMethod]
    public async Task SimulationConfirmHostOrderConsumerTest()
    {
        var fixture = new MassTransitConsumerTestFixture<SimulationConfirmHostOrderConsumer>();

        string orderId = "TestOrder123";
        var correlationId = orderId.ToGuid();

        await fixture.TestHarness!.Bus.Publish(new HostOrderHandlingSucceeded(correlationId, orderId));

        (await fixture.TestHarness.Published.Any<HostOrderHandlingSucceeded>()).ShouldBeTrue();
        (await fixture.TestHarness.Consumed.Any<HostOrderHandlingSucceeded>()).ShouldBeTrue();

        var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderHandlingSucceeded>().First().Context.Message;

        message.CorrelationId.ShouldBe(correlationId);
        message.OrderId.ShouldBe(orderId);
    }
}