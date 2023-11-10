using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
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
            AgvPosition = new MqttAgvPosition()
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
        const string serialNumber = "AGV003";
        var fixture = new AgvStateConsumerTestFixture()
            .UseDefaultServices()
            .WithAgvInformation(AgvInformationGenerator.GetAgvInformation(2, serialNumber.ToGuid()))
            .Build();

        const string orderId = "Order123";
        var batteryState = new MqttBatteryState
        {
            Charging = false
        };

        // Act
        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(serialNumber, orderId) // Initially, AGV003 has _chargingState = true
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
        const string serialNumber = "AGV002";
        var fixture = new AgvStateConsumerTestFixture()
            .UseDefaultServices()
            .WithAgvInformation(AgvInformationGenerator.GetAgvInformation(agvCorrelationId: serialNumber.ToGuid()))
            .Build();

        const string orderId = "Order123";
        var batteryState = new MqttBatteryState
        {
            Charging = true
        };

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(serialNumber, orderId) // Initially, AGV002 has _chargingState = false
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
    public async Task ReceiveMqttState_ChargingStateExamination_RepositoryEmpty()
    {
        // Arrange
        var fixture = new AgvStateConsumerTestFixture()
            .EmptyRepository()
            .UseDefaultServices()
            .Build();

        const string orderId = "Order123";
        var batteryState = new MqttBatteryState
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

        var agv = fixture.AgvRepository.GetAgvInformation(AgvTestIdentifier02.ToGuid());
        agv.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task ReceiveMqttState_WhenMessageHasErrors_PublishAgvErrorStatusReceived()
    {
        const string serialNumber = "AGV002";
        const string orderId = "Order123";
        var fixture = new AgvStateConsumerTestFixture()
            .UseDefaultServices()
            .WithAgvInformation(AgvInformationGenerator.GetAgvInformation(agvCorrelationId: serialNumber.ToGuid()))
            .Build();
        var error = new MqttError
        {
            ErrorDescription = "error description",
            ErrorLevel = MqttErrorLevel.Fatal,
            ErrorReferences = new List<MqttErrorReference>
            {
                new() { ReferenceKey = "testKey", ReferenceValue = "testValue" }
            },
            ErrorType = "error type"
        };
        var expectedErrors = new List<MqttError>
        {
            error
        };

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(serialNumber, orderId) // Initially, AGV002 has _chargingState = false
        {
            Errors = expectedErrors
        }, null);

        (await fixture.TestHarness.Published.Any<MqttState>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<MqttState>()).ShouldBeTrue();

        var agvErrorStatus = fixture.TestHarness.Published.Select<AgvErrorStatusReceived>().First().Context.Message;
        agvErrorStatus.Errors.ShouldNotBeNull();
        agvErrorStatus.Errors.Count.ShouldBe(1);
        agvErrorStatus.Errors.First().ShouldSatisfyAllConditions(
            e => e.ErrorDescription.ShouldBe(error.ErrorDescription),
            e => e.ErrorLevel.ToString().ShouldBe(error.ErrorLevel.ToString()),
            e => e.ErrorType.ShouldBe(error.ErrorType),
            e => e.ErrorReferences.ShouldNotBeNull(),
            e => e.ErrorReferences!.First().ShouldSatisfyAllConditions(
                r => r.ReferenceKey.ShouldBe(error.ErrorReferences!.First().ReferenceKey),
                r => r.ReferenceValue.ShouldBe(error.ErrorReferences!.First().ReferenceValue)));
    }
}