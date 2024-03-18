using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages.SimulateAgv;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class SimulatedAgvErrorConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttSimulateAgvErrorWasReceived_PublishesSimulateAgvError()
    {
        MqttConsumerTestFixture<SimulateAgvErrorConsumer> fixture = new MqttConsumerTestFixture<SimulateAgvErrorConsumer>(s
                => s.AddTransient(_ => new Mock<ILogger<SimulateAgvErrorConsumer>>().Object))
            .Build();

        var agvCorrelationId = Guid.NewGuid();
        var agvError = AgvErrorGenerator.GenerateAgvError(agvCorrelationId);
        await fixture.TestHarness.Bus.Publish(new MqttSimulateAgvError { CorrelationId = agvCorrelationId, AgvErrors = new List<AgvError> { agvError } });

        (await fixture.ConsumerHarness.Consumed.Any<MqttSimulateAgvError>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<SimulateAgvError>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<SimulateAgvError>().First().Context.Message;
        message.CorrelationId.ShouldBe(agvCorrelationId);
        message.AgvErrors.ShouldNotBeNull();
        message.AgvErrors.Count.ShouldBe(1);
        message.AgvErrors.First().ShouldSatisfyAllConditions(
            e => e.ErrorDescription.ShouldBe(agvError.ErrorDescription),
            e => e.ErrorLevel.ToString().ShouldBe(agvError.ErrorLevel.ToString()),
            e => e.ErrorType.ShouldBe(agvError.ErrorType),
            e => e.ErrorReferences.ShouldNotBeNull(),
            e => e.ErrorReferences!.First().ShouldSatisfyAllConditions(
                r => r.ReferenceKey.ShouldBe(agvError.ErrorReferences!.First().ReferenceKey),
                r => r.ReferenceValue.ShouldBe(agvError.ErrorReferences!.First().ReferenceValue)));
    }
}