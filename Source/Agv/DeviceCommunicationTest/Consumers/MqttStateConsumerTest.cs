using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

[TestClass]
public class MqttStateConsumerTest
{
    private const string AgvSerialNumber = "#0815#";

    /// <summary>
    ///  Test for consuming invalid AGV status message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveStatusMessage_WithInvalidContent_NoMessageSend()
    {
        var fixture = new AgvStateConsumerTestFixture();
        await fixture.TestHarness.Bus.PublishMqtt(new MqttState("", ""), null);

        await fixture.ShouldNotHavePublishedAsync<AgvStatusReceived>();
    }

    /// <summary>
    /// Test for consuming AGV graph message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveGraphMessage_WithValidInput_ExpectMessage()
    {
        var fixture = new AgvStateConsumerTestFixture();
        const string agvTestIdentifier = "#0815#";
        const string orderId = "Order123";

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(agvTestIdentifier, orderId), null);
        await fixture.ShouldHavePublishedAsync<MqttState>();

        var consumedState = await fixture.GetFirstConsumedMessageAsync<MqttState>();
        consumedState.SerialNumber.ShouldBe(agvTestIdentifier);

        await fixture.ShouldHavePublishedAsync<AgvStatusReceived>();

        var publishedStatus = await fixture.GetFirstPublishedMessageAsync<AgvStatusReceived>();
        publishedStatus.CorrelationId.ShouldBe(agvTestIdentifier.ToGuid());
        publishedStatus.SerialNumber.ShouldBe(agvTestIdentifier);
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
        var fixture = new AgvStateConsumerTestFixture();
        const string orderId = "Order123";

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(AgvSerialNumber, orderId)
        {
            LastNodeId = sentLastNodeId,
            AgvPosition = null
        }, null);

        await fixture.ShouldHavePublishedAsync<MqttState>();

        var consumedState = await fixture.GetFirstConsumedMessageAsync<MqttState>();
        consumedState.LastNodeId.ShouldBe(sentLastNodeId);

        var publishedStatus = await fixture.GetFirstPublishedMessageAsync<AgvStatusReceived>();
        publishedStatus.AgvStatus.LastNodeId.ShouldBe(receivedLastNodeId);

        await fixture.ShouldNotHavePublishedAsync<AgvPositionReceived>();
    }

    [TestMethod]
    [DataRow(null, false, 4.56f, 7.89f, 3.14159f)]
    [DataRow(0.0123f, true, 4.56f, 7.89f, 3.14159f)]
    public async Task ReceiveStateWithPosition_ExpectAdditionalAgvPositionReceived(float? deviationRange, bool positionInitialized, float x, float y, float theta)
    {
        var fixture = new AgvStateConsumerTestFixture();
        const string orderId = "Order123";

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(AgvSerialNumber, orderId)
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

        await fixture.ShouldHavePublishedAsync<MqttState>();

        var consumedState = await fixture.GetFirstConsumedMessageAsync<MqttState>();
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

        var publishedPos = await fixture.GetFirstPublishedMessageAsync<AgvPositionReceived>();
        publishedPos.CorrelationId.ShouldBe(AgvSerialNumber.ToGuid());
        publishedPos.SerialNumber.ShouldBe(AgvSerialNumber);
        publishedPos.AgvPosition.PositionInitialized.ShouldBe(positionInitialized);
        publishedPos.AgvPosition.Theta.ShouldBe((float)Mathematics.ConvertToRadians(90) - theta);
        publishedPos.AgvPosition.X.ShouldBe(x);
        (publishedPos.AgvPosition.Y * -1).ShouldBe(y);
    }

    [TestMethod]
    public async Task ReceiveMqttState_WhenMessageHasErrors_PublishAgvErrorStatusReceived()
    {
        const string serialNumber = "AGV002";
        const string orderId = "Order123";
        var fixture = new AgvStateConsumerTestFixture()
            .WithAgvInformation(AgvInformationGenerator.GetAgvInformation(agvCorrelationId: serialNumber.ToGuid()));

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
            Errors = expectedErrors,
            AgvPosition = new MqttAgvPosition()
            {
                PositionInitialized = true,
                Theta = 0,
                X = 12,
                Y = 12
            }
        }, null);

        await fixture.ShouldHavePublishedAsync<MqttState>();
        await fixture.ShouldHaveConsumedAsync<MqttState>();
        
        var agvErrorStatus = await fixture.GetFirstPublishedMessageAsync<AgvErrorStatusReceived>();
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

    /// <summary>
    /// Test for consuming MqttState message with load/cargo.
    /// </summary>
    [TestMethod]
    public async Task ReceiveStateWithLoad_ExpectAgvLoadStatusReceived()
    {
        var fixture = new AgvStateConsumerTestFixture();
        const string agvTestIdentifier = "TestAgv-1";
        const string orderId = "TestOrder123";
        const string cargoId = "Cargo-";
        const float dimensionHeight = 0.2f;

        var loadList = new List<MqttLoad>
        {
            new() { LoadId = cargoId + "1", LoadPosition = "positionC1", LoadType = "Box", LoadDimensions = new MqttLoadDimensions { Height = dimensionHeight, Width = 0.8f, Length = 0.9f }, BoundingBoxReference = null },
            new() { LoadId = cargoId + "2", LoadPosition = "positionC2", LoadType = "Box", LoadDimensions = new MqttLoadDimensions { Height = dimensionHeight, Width = 0.8f, Length = 0.9f }, BoundingBoxReference = null }
        };

        await fixture.TestHarness.Bus.PublishMqtt(new MqttState(agvTestIdentifier, orderId) {
             Loads = loadList,
               AgvPosition = new MqttAgvPosition()
            {
                PositionInitialized = true,
                Theta = 0,
                X = 12,
                Y = 12
            }
            }, null);
        await fixture.ShouldHavePublishedAsync<MqttState>();
        
        var consumedState = await fixture.GetFirstConsumedMessageAsync<MqttState>();
        consumedState.SerialNumber.ShouldBe(agvTestIdentifier);

        await fixture.ShouldHavePublishedAsync<AgvStatusReceived>();
        await fixture.ShouldHavePublishedAsync<AgvLoadStatusReceived>();

        var publishedLoadStatus = await fixture.GetFirstPublishedMessageAsync<AgvLoadStatusReceived>();
        publishedLoadStatus.Cargos.Count.ShouldBe(2);
        publishedLoadStatus.Cargos.First().LoadId.ShouldBe(cargoId + "1");
        publishedLoadStatus.Cargos.First().Dimension!.Height.ShouldBe(dimensionHeight);
    }
}