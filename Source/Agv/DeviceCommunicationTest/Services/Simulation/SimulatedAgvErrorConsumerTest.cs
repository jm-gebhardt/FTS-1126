using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Commands;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class SimulatedAgvErrorConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttSimulateAgvErrorWasReceived_PublishesSimulateAgvError()
    {
        MqttConsumerTestFixture<MqttSimulateAgvErrorConsumer> fixture = new SimulateAgvErrorConsumerFixture();

        var agvCorrelationId = Guid.NewGuid();
        var agvError = AgvErrorGenerator.GenerateAgvError(agvCorrelationId);

        var message = await fixture.PublishAndGetFirstMessageAsync<MqttSimulateAgvError, SimulateAgvError>(
            new MqttSimulateAgvError { CorrelationId = agvCorrelationId, AgvErrors = new List<AgvError> { agvError } });
        
        await fixture.ShouldHaveConsumedAsync<MqttSimulateAgvError>();
        
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

public class SimulateAgvErrorConsumerFixture : MqttConsumerTestFixture<MqttSimulateAgvErrorConsumer>
{
    public SimulateAgvErrorConsumerFixture()
    {
        AddServices(s => s.AddTransient(_ => new Mock<ILogger<MqttSimulateAgvErrorConsumer>>().Object));
        Build();
    }
}