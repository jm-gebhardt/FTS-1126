using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

[TestClass]
public class StopChargingCommandConsumerTest
{
    [TestMethod]
    public async Task ConsumeStopChargingCommand_WhenInCorrectState_PublishesAgvStoppedCharging()
    {
        var correlationId = "AGV003".ToGuid();
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        agvInformation.AgvStatusReceived = AgvInformationGenerator.GetAgvStatusReceived(new AgvStatus(correlationId, "orderId", false, true, 0, true, false, "CRG2"));

        MediatorTestFixture<StopChargingCommand, StopChargingCommandConsumer> fixture = new StopChargingCommandConsumerTestFixture()
            .AddAgvInformation(correlationId, agvInformation)
            .Build();

        var stopChargingCommand = new StopChargingCommand(correlationId);
        await fixture.GetResponseAsync<BaseResult>(stopChargingCommand);

        bool publishCheck = await fixture.TestHarness!.Published.Any<AgvStoppedCharging>();
        publishCheck.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ConsumeStopChargingCommand_WhenAgvNoAgvStatus_ThrowsNoContentException()
    {
        var correlationId = Guid.NewGuid();
        MediatorTestFixture<StopChargingCommand, StopChargingCommandConsumer> fixture = new StopChargingCommandConsumerTestFixture().AddAgvInformationWithSpecificAgvStatus(correlationId, null)
            .Build();

        var stopChargingCommand = new StopChargingCommand(correlationId);
        await MediatorShould.ThrowRequestExceptionAsync<AgvStatusReceivedNotFoundException>(async () => await fixture.GetResponseAsync<BaseResult>(stopChargingCommand));

        bool publishCheck = await fixture.TestHarness!.Published.Any<AgvStoppedCharging>();
        publishCheck.ShouldBeFalse();
    }

    [TestMethod]
    public async Task ConsumeStopChargingCommand_WhenAgvNotInChargingState_ThrowsNotInCorrectStateException()
    {
        const bool isCharging = false;
        var correlationId = Guid.NewGuid();
        var agvStatus = new AgvStatus(correlationId, "TestOrder", false, true, 25, isCharging, false, "CRG1");
        MediatorTestFixture<StopChargingCommand, StopChargingCommandConsumer> fixture = new StopChargingCommandConsumerTestFixture().AddAgvInformationWithSpecificAgvStatus(correlationId, agvStatus)
            .Build();

        var stopChargingCommand = new StopChargingCommand(correlationId);
        await MediatorShould.ThrowRequestExceptionAsync<NotInCorrectStateException>(async () => await fixture.GetResponseAsync<BaseResult>(stopChargingCommand));

        bool publishCheck = await fixture.TestHarness!.Published.Any<AgvStoppedCharging>();
        publishCheck.ShouldBeFalse();
    }
}