using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

[TestClass]
public class AgvStateConsumerTest
{
    private const string AgvTestIdentifier02 = "#0815#";

    /// <summary>
    ///  Test for consuming invalid AGV status message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveStatusMessage_WithInvalidContent_NoMessageSend()
    {
        var fixture = new AgvStateConsumerTestFixture()
                    .UseDefaultServices()
                    .Build();
        await fixture.TestHarness.Bus.PublishMqtt(new MqttState("", ""), null);

        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeFalse();
    }

    /// <summary>
    /// Test for consuming AGV graph message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveGraphMessage_WithValidInput_ExpectMessage()
    {
        var fixture = new AgvStateConsumerTestFixture().UseDefaultServices().Build();
        const string agvTestIdentifier = "#0815#";
        const string orderId = "Order123";

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(agvTestIdentifier, orderId), null);
        (await fixture.TestHarness.Published.Any<MqttState>()).ShouldBeTrue();

        var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
        consumedState.SerialNumber.ShouldBe(agvTestIdentifier);

        (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();

        var publishedStatus = fixture.TestHarness.Published.Select<AgvStatusReceived>().First().Context.Message;
        publishedStatus.CorrelationId.ShouldBe(agvTestIdentifier.ToGuid());
        publishedStatus.AgvId.ShouldBe(agvTestIdentifier);
    }

    /// <summary>
    /// Test for publishing IAgvStatusReceived message with LastNodeId.
    /// </summary>
    [TestMethod]
    [DataRow("Node01", "Node01")]
    [DataRow(null, null)]
    [DataRow("", null)]
    public async Task ReceiveStateWithLastNode_ExpectLastNodeTransferred(string sentLastNodeId, string receivedLastNodeId)
    {
        var fixture = new AgvStateConsumerTestFixture().UseDefaultServices().Build();
        const string orderId = "Order123";

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(AgvTestIdentifier02, orderId)
        {
            LastNodeId = sentLastNodeId,
            AgvPosition = null
        }, null);

        (await fixture.TestHarness.Published.Any<MqttState>()).ShouldBeTrue();

        var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
        consumedState.LastNodeId.ShouldBe(sentLastNodeId);

        var publishedStatus = fixture.TestHarness.Published.Select<AgvStatusReceived>().First().Context.Message;
        publishedStatus.AgvStatus.LastNodeId.ShouldBe(receivedLastNodeId);

        (await fixture.TestHarness.Published.Any<AgvPositionReceived>()).ShouldBeFalse();
    }

    [TestMethod]
    [DataRow(null, false, 4.56f, 7.89f, 3.14159f)]
    [DataRow(0.0123f, true, 4.56f, 7.89f, 3.14159f)]
    public async Task ReceiveStateWithPosition_ExpectAdditionalAgvPositionReceived(float? deviationRange, bool positionInitialized, float x, float y, float theta)
    {
        var fixture = new AgvStateConsumerTestFixture().UseDefaultServices().Build();
        const string orderId = "Order123";

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(AgvTestIdentifier02, orderId)
        {
            AgvPosition = new AgvPosition()
            {
                DeviationRange = deviationRange,
                PositionInitialized = positionInitialized,
                Theta = theta,
                X = x,
                Y = y
            }
        }, null);

        (await fixture.TestHarness.Published.Any<MqttState>()).ShouldBeTrue();

        var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
        consumedState.AgvPosition.ShouldNotBeNull();
        if (deviationRange == null)
        {
            consumedState.AgvPosition.DeviationRange.ShouldBeNull();
        }
        else
        {
            consumedState.AgvPosition.DeviationRange!.Value.ShouldBe(deviationRange.Value, 0.00001);
        }
        consumedState.AgvPosition.PositionInitialized.ShouldBe(positionInitialized);
        consumedState.AgvPosition.Theta.ShouldBe(theta, 0.00001);
        consumedState.AgvPosition.X.ShouldBe(x, 0.00001);
        consumedState.AgvPosition.Y.ShouldBe(y, 0.00001);

        var publishedPos = fixture.TestHarness.Published.Select<AgvPositionReceived>().First().Context.Message;
        publishedPos.CorrelationId.ShouldBe(AgvTestIdentifier02.ToGuid());
        publishedPos.AgvId.ShouldBe(AgvTestIdentifier02);
        publishedPos.AgvPosition.PositionInitialized.ShouldBe(positionInitialized);
        publishedPos.AgvPosition.Theta.ShouldBe(Mathematics.ConvertToRadians(90) - theta);
        publishedPos.AgvPosition.X.ShouldBe(x);
        (publishedPos.AgvPosition.Y * -1).ShouldBe(y);
    }

    [TestMethod]
    public async Task ReceiveMqttState_ChargingStateExamination_AgvStoppedCharging_Sent()
    {
        // Arrange
        var fixture = new AgvStateConsumerTestFixture().UseDefaultServices().Build();
        
        const string orderId = "Order123";
        var batteryState = new BatteryState
        {
            Charging = false
        };

        // Act
        await fixture.TestHarness.Bus.PublishMqtt(new MqttState("AGV003", orderId)      // Initially, AGV003 has _chargingState = true
        {
            BatteryState = batteryState
        }, null);

        // Assert
        (await fixture.TestHarness.Published.Any<MqttState>()).ShouldBeTrue();

        var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
        consumedState.BatteryState.ShouldBe(batteryState);

        (await fixture.TestHarness.Published.Any<AgvStoppedCharging>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ReceiveMqttState_ChargingStateExamination_AgvStartedCharging_Sent()
    {
        // Arrange
        var fixture = new AgvStateConsumerTestFixture().UseDefaultServices().Build();

        const string orderId = "Order123";
        var batteryState = new BatteryState
        {
            Charging = true
        };

        // Act
        await fixture.TestHarness.Bus.PublishMqtt(new MqttState("AGV002", orderId)      // Initially, AGV002 has _chargingState = false
        {
            BatteryState = batteryState
        }, null);

        // Assert
        (await fixture.TestHarness.Published.Any<MqttState>()).ShouldBeTrue();

        var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
        consumedState.BatteryState.ShouldBe(batteryState);

        (await fixture.TestHarness.Published.Any<AgvStartedCharging>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ReceiveMqttState_ChargingStateExamination_RegistryEmpty()
    {
        // Arrange
        var fixture = new AgvStateConsumerTestFixture().EmptyRegistry().Build();

        const string orderId = "Order123";
        var batteryState = new BatteryState
        {
            Charging = false
        };

        // Act
        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(AgvTestIdentifier02, orderId)
        {
            BatteryState = batteryState
        }, null);

        // Assert
        (await fixture.TestHarness.Published.Any<MqttState>()).ShouldBeTrue();

        (await fixture.ConsumerHarness.Consumed.Any<MqttState>()).ShouldBeTrue();

        var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
        consumedState.BatteryState.ShouldBe(batteryState);

        (await fixture.TestHarness.Published.Any<AgvStartedCharging>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AgvStoppedCharging>()).ShouldBeFalse();
    }
}