using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.CommonTest.Helpers;
using Shouldly;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

[TestClass]
public class StartChargingCommandConsumerTest
{
    [TestMethod]
    public async Task ConsumeStartChargingCommand_WhenInCorrectState_PublishesStartActionCommand()
    {
        var correlationId = "AGV004".ToGuid();
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        agvInformation.AgvStatusReceived = AgvInformationGenerator.GetAgvStatusReceived(new AgvStatus(correlationId, "orderId", false, true, 0, false, false, "CRG2"));
        var fixture = new StartChargingCommandConsumerTestFixture()
            .AddAgvInformation(correlationId, agvInformation)
            .Build();
        string? lastNodeId = fixture.GetAgvInformation(correlationId)?.AgvStatusReceived?.AgvStatus.LastNodeId;

        var startChargingCommand = new StartChargingCommand(correlationId);
        await fixture.GetResponseAsync<BaseResult>(startChargingCommand);

        bool publishCheck = await fixture.TestHarness!.Published.Any<StartActionCommand>(s => s.Context.Message.Action.ActionType == ActionType.Charge
                                                                                              && s.Context.Message.Action.TargetNodeId == lastNodeId);
        publishCheck.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ConsumeStartChargingCommand_WhenAgvBusy_ThrowsNotInCorrectStateException()
    {
        const bool isBusy = true;
        var correlationId = Guid.NewGuid();
        var agvStatus = new AgvStatus(correlationId, "TestOrder", isBusy, true, 25, false, false, "CRG1");
        MediatorTestFixture<StartChargingCommand, StartChargingCommandConsumer> fixture = new StartChargingCommandConsumerTestFixture().AddAgvInformationWithSpecificAgvStatus(correlationId, agvStatus)
            .Build();

        var startChargingCommand = new StartChargingCommand(correlationId);
        await MediatorShould.ThrowRequestExceptionAsync<NotInCorrectStateException>(async () => await fixture.GetResponseAsync<BaseResult>(startChargingCommand));

        bool publishCheck = await fixture.TestHarness!.Published.Any<StartActionCommand>(s => s.Context.Message.Action.ActionType == ActionType.Charge);
        publishCheck.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow(100)]
    [DataRow(120)]
    public async Task ConsumeStartChargingCommand_WhenAgvBatteryChargeIsGreaterOrEqual100_ThrowsNotInCorrectStateException(int batteryCharge)
    {
        var correlationId = Guid.NewGuid();
        var agvStatus = new AgvStatus(correlationId, "TestOrder", false, true, batteryCharge, false, false, "CRG1");
        MediatorTestFixture<StartChargingCommand, StartChargingCommandConsumer> fixture = new StartChargingCommandConsumerTestFixture().AddAgvInformationWithSpecificAgvStatus(correlationId, agvStatus)
            .Build();

        var startChargingCommand = new StartChargingCommand(correlationId);
        await MediatorShould.ThrowRequestExceptionAsync<NotInCorrectStateException>(async () => await fixture.GetResponseAsync<BaseResult>(startChargingCommand));

        bool publishCheck = await fixture.TestHarness!.Published.Any<StartActionCommand>(s => s.Context.Message.Action.ActionType == ActionType.Charge);
        publishCheck.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("SRC1")]
    [DataRow("DST1")]
    public async Task ConsumeStartChargingCommand_WhenAgvCurrentNodeIsNotChargeNode_ThrowsNotInCorrectStateException(string lastNodeId)
    {
        var correlationId = Guid.NewGuid();
        var agvStatus = new AgvStatus(correlationId, "TestOrder", false, true, 25, false, false, lastNodeId);
        MediatorTestFixture<StartChargingCommand, StartChargingCommandConsumer> fixture = new StartChargingCommandConsumerTestFixture()
            .AddAgvInformationWithSpecificAgvStatus(correlationId, agvStatus)
            .Build();

        var startChargingCommand = new StartChargingCommand(correlationId);
        await MediatorShould.ThrowRequestExceptionAsync<NotInCorrectStateException>(async () => await fixture.GetResponseAsync<BaseResult>(startChargingCommand));

        bool publishCheck = await fixture.TestHarness!.Published.Any<StartActionCommand>(s => s.Context.Message.Action.ActionType == ActionType.Charge);
        publishCheck.ShouldBeFalse();
    }

    [TestMethod]
    public async Task ConsumeStartChargingCommand_WhenAgvHasNoAgvInformation_ThrowsNoContentException()
    {
        var correlationId = "NotExisting".ToGuid();
        MediatorTestFixture<StartChargingCommand, StartChargingCommandConsumer> fixture = new StartChargingCommandConsumerTestFixture()
            .Build();

        var startChargingCommand = new StartChargingCommand(correlationId);
        await MediatorShould.ThrowRequestExceptionAsync<AgvInformationNotFoundException>(async () => await fixture.GetResponseAsync<BaseResult>(startChargingCommand));

        bool publishCheck = await fixture.TestHarness!.Published.Any<StartActionCommand>(s => s.Context.Message.Action.ActionType == ActionType.Charge);
        publishCheck.ShouldBeFalse();
    }

    [TestMethod]
    public async Task ConsumeStartChargingCommand_WhenAgvHasNoAgvStatus_ThrowsNoContentException()
    {
        var correlationId = Guid.NewGuid();
        MediatorTestFixture<StartChargingCommand, StartChargingCommandConsumer> fixture = new StartChargingCommandConsumerTestFixture().AddAgvInformationWithSpecificAgvStatus(correlationId, null)
            .Build();

        var startChargingCommand = new StartChargingCommand(correlationId);
        await MediatorShould.ThrowRequestExceptionAsync<AgvStatusReceivedNotFoundException>(async () => await fixture.GetResponseAsync<BaseResult>(startChargingCommand));

        bool publishCheck = await fixture.TestHarness!.Published.Any<StartActionCommand>(s => s.Context.Message.Action.ActionType == ActionType.Charge);
        publishCheck.ShouldBeFalse();
    }
}