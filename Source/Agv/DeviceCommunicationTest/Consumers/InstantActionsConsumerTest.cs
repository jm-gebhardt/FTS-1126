using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

[TestClass]
public class InstantActionsConsumerTest
{
    [TestMethod]
    public async Task Consume_StartInstantAction_ValidData_PublishesMqttInstantAction()
    {
        var fixture = new InstantActionConsumerTestFixture();
        var startInstantAction = new StartInstantAction { AgvInstantActionType = "action type", CorrelationId = Guid.NewGuid(), AgvSerialNumber = "agv" };
      
        var message = await fixture.PublishAndGetFirstMessageAsync<StartInstantAction, MqttInstantActions>(
            startInstantAction);
        
        message.Actions.Count.ShouldBe(1);
        message.SerialNumber.ShouldBe(startInstantAction.AgvSerialNumber);
        message.Actions.First().ActionType.ShouldBe(startInstantAction.AgvInstantActionType);
    }

    [TestMethod]
    public async Task Consume_StartInstantAction_NoAgvSerialNumber_DoesNotPublishMqttInstantAction()
    {
        var fixture = new InstantActionConsumerTestFixture();
        var startInstantAction = new StartInstantAction { AgvInstantActionType = "action type", CorrelationId = Guid.NewGuid(), AgvSerialNumber = string.Empty };
        await fixture.TestHarness.Bus.Publish(startInstantAction);

        await fixture.ShouldHaveConsumedAsync<StartInstantAction>();
        await fixture.ShouldNotHavePublishedAsync<MqttInstantActions>();
    }
}