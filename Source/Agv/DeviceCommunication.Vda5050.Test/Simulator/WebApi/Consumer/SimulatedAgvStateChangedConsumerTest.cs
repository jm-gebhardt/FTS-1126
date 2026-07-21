using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Consumer;

[TestClass]
public class SimulatedAgvStateChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_FilterAllows_PublishesStateWithCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var fixture = new SimulatedAgvStateChangedConsumerTestFixture();
        var state = MqttStateGenerator.Generate(SimulatedAgvStateChangedConsumerTestFixture.SerialNumber);

        await fixture.PublishMessageAsync(new SimulatedAgvStateChanged(correlationId, state));

        fixture.VerifyPublished(Times.Once());
        fixture.PublishedState.ShouldNotBeNull();
        fixture.PublishedState!.CorrelationId.ShouldBe(correlationId);
        fixture.PublishedState!.SerialNumber.ShouldBe(SimulatedAgvStateChangedConsumerTestFixture.SerialNumber);
    }

    [TestMethod]
    public async Task Consume_FilterRejects_DoesNotPublish()
    {
        var fixture = new SimulatedAgvStateChangedConsumerTestFixture(false);
        var state = MqttStateGenerator.Generate();

        await fixture.PublishMessageAsync(new SimulatedAgvStateChanged(Guid.NewGuid(), state));

        fixture.VerifyPublished(Times.Never());
    }
}