using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.MqttInstantActionsConsumerTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

[TestClass]
public class MqttInstantActionsConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttInstantActions_NonSimulatedAgv_DoesNotPublishAnyCommand()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            "other",
            "other0",
            [MqttInstantActionsGenerator.GenerateCancelOrderAction()]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldNotHavePublishedAsync<AbortOrder>();
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_InitPosition_PublishesUpdatePosition()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            actions: [MqttInstantActionsGenerator.GenerateInitPositionAction(1.0f, 2.0f, 0.5f)]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<UpdatePosition>();
        var published = await fixture.GetFirstPublishedMessageAsync<UpdatePosition>();
        published.Position.X.ShouldBe(1.0f);
        published.Position.Y.ShouldBe(2.0f);
        published.Position.Theta.ShouldBe(0.5f);
        published.Position.PositionInitialized.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_InitPosition_WithLastNodeId_PublishesUpdatePositionWithLastNodeId()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            actions: [MqttInstantActionsGenerator.GenerateInitPositionAction(lastNodeId: "node1")]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<UpdatePosition>();
        var published = await fixture.GetFirstPublishedMessageAsync<UpdatePosition>();
        published.LastNodeId.ShouldBe("node1");
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_InitPosition_NullActionParameters_DoesNotPublishUpdatePosition()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            actions:
            [
                new MqttInstantAction
                {
                    ActionType = Vda5050Specifications.ActionTypes.InitPosition,
                    ActionId = "action1",
                    ActionParameters = null
                }
            ]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldNotHavePublishedAsync<UpdatePosition>();
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_InitPosition_MissingCoordinates_DoesNotPublishUpdatePosition()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            actions:
            [
                new MqttInstantAction
                {
                    ActionType = Vda5050Specifications.ActionTypes.InitPosition,
                    ActionId = "action1",
                    ActionParameters = [new MqttActionParameter { Key = Vda5050Specifications.Parameters.X, Value = 1.0f }]
                }
            ]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldNotHavePublishedAsync<UpdatePosition>();
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_StartCharging_PublishesStartCharging()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            actions: [MqttInstantActionsGenerator.GenerateStartChargingAction()]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<StartCharging>();
        await fixture.ShouldNotHavePublishedAsync<StopCharging>();
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_StopCharging_PublishesStopCharging()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            actions: [MqttInstantActionsGenerator.GenerateStopChargingAction()]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<StopCharging>();
        await fixture.ShouldNotHavePublishedAsync<StartCharging>();
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_CancelOrder_PublishesAbortOrder()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        const string actionId = "cancelAction1";
        var message = MqttInstantActionsGenerator.Generate(
            actions: [MqttInstantActionsGenerator.GenerateCancelOrderAction()]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AbortOrder>();
        var published = await fixture.GetFirstPublishedMessageAsync<AbortOrder>();
        published.ActionId.ShouldBe(actionId);
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_FactsheetRequest_PublishesRequestFactsheet()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            actions: [MqttInstantActionsGenerator.GenerateFactsheetRequestAction()]);

        await fixture.PublishMqttMessageAsync(message);

        var published = await fixture.GetFirstPublishedMessageAsync<RequestFactsheet>();
        published.CorrelationId.ShouldBe(Fixture.SimulatedSerialNumber.ToGuid());
        published.ActionId.ShouldBe(MqttInstantActionsGenerator.FactsheetRequestActionId);
    }

    [TestMethod]
    public async Task Consume_MqttInstantActions_TwoFactsheetRequests_PublishesRequestFactsheetForEach()
    {
        var fixture = new MqttInstantActionsConsumerTestFixture();
        var message = MqttInstantActionsGenerator.Generate(
            actions:
            [
                MqttInstantActionsGenerator.GenerateFactsheetRequestAction(),
                MqttInstantActionsGenerator.GenerateFactsheetRequestAction(MqttInstantActionsGenerator.SecondFactsheetRequestActionId)
            ]);

        await fixture.PublishMqttMessageAsync(message);

        (await fixture.GetFirstPublishedMessageAsync<RequestFactsheet>(2)).ShouldNotBeNull();
        fixture.GetPublishedMessages<RequestFactsheet>().Select(r => r.ActionId)
            .ShouldBe([MqttInstantActionsGenerator.FactsheetRequestActionId, MqttInstantActionsGenerator.SecondFactsheetRequestActionId], true);
    }
}