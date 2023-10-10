using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using AgvData = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvData;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;
using AgvStatus = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

[TestClass]
public class AgvCommandHandlerTest
{
    private readonly IMapper _mapper = AgvCommandHandlerTestFixture.GetMapper();

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_ValidData_SetsAgvIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateChargingAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        fixture.SetupAgv(hasPendingActions: true);

        await fixture.TestHarness!.Bus.Publish(new TryReactivateChargingAgv());
        (await fixture.ConsumerHarness!.Consumed.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
        fixture.AgvRepository.Verify(r =>
            r.UpdateAgvAsync(It.Is<TransportOrder.Domain.Models.Agv.Agv>(a => agv.CorrelationId == a.CorrelationId && a.CurrentAgvState == AgvState.Idle)), Times.Once);
    }

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_NoPendingAction_AgvNotSetIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateChargingAgv();

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        fixture.SetupAgv(hasPendingActions: false);

        await fixture.TestHarness!.Bus.Publish(new TryReactivateChargingAgv());
        (await fixture.ConsumerHarness!.Consumed.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
        fixture.AgvRepository.Verify(r =>
            r.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), Times.Never());
    }

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_NoChargingAgvs_AgvNotSetIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>());
        await fixture.TestHarness!.Bus.Publish(new TryReactivateChargingAgv());
        (await fixture.ConsumerHarness!.Consumed.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
        fixture.AgvRepository.Verify(r =>
            r.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_AgvWithoutErrors_PublishesAgvPropertyChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.TestHarness!.Bus.Publish(new UpdateErrorStatus(agv.CorrelationId, new List<AgvError>{new(agv.CorrelationId, "description", ErrorLevel.Warning, null, null, DateTime.UtcNow)}));
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateErrorStatus>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_AgvWithErrors_AgvPropertyChangedNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.HasError = true;
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.TestHarness!.Bus.Publish(new UpdateErrorStatus(agv.CorrelationId, new List<AgvError>()));
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateErrorStatus>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CreateAgv_GraphExists_PublishesRequestAgvDataAndAgvStateChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.SetupAgv(graphExists: true);
        var agvStatus = new AgvStatus(Guid.NewGuid(), null, false, true, 100.0d, false, false);
        var createAgv = new CreateAgv(agvStatus.CorrelationId, "agvId", agvStatus);

        await fixture.TestHarness!.Bus.Publish(createAgv);
        (await fixture.ConsumerHarness!.Consumed.Any<CreateAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<Common.Messages.AgvManagement.Commands.RequestAgvData>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<Common.Messages.AgvManagement.Commands.RequestAgvGraph>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_CreateAgv_GraphDoesNotExist_PublishesRequestAgvDataRequestAgvGraphAndAgvStateChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.SetupAgv(graphExists: false);
        var agvStatus = new AgvStatus(Guid.NewGuid(), null, false, true, 100.0d, false, false);
        var createAgv = new CreateAgv(agvStatus.CorrelationId, "agvId", agvStatus);

        await fixture.TestHarness!.Bus.Publish(createAgv);
        (await fixture.ConsumerHarness!.Consumed.Any<CreateAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<Common.Messages.AgvManagement.Commands.RequestAgvData>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<Common.Messages.AgvManagement.Commands.RequestAgvGraph>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StartCharging_KnownIdleAgv_PublishesAgvStateChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.TestHarness!.Bus.Publish(new StartCharging(agv.CorrelationId));
        (await fixture.ConsumerHarness!.Consumed.Any<StartCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
        agv.CurrentAgvState.ShouldBe(AgvState.Charging);
    }

    [TestMethod]
    public async Task Consume_StartCharging_UnknownAgv_AgvStateChangedNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();

        await fixture.TestHarness!.Bus.Publish(new StartCharging(Guid.NewGuid()));
        (await fixture.ConsumerHarness!.Consumed.Any<StartCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_StopCharging_KnownChargingAgv_PublishesAgvStateChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.TransitionToCharging();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.TestHarness!.Bus.Publish(new StopCharging(agv.CorrelationId));
        (await fixture.ConsumerHarness!.Consumed.Any<StopCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
        agv.CurrentAgvState.ShouldBe(AgvState.Idle);
    }

    [TestMethod]
    public async Task Consume_StopCharging_UnknownAgv_AgvStateChangedNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();

        await fixture.TestHarness!.Bus.Publish(new StopCharging(Guid.NewGuid()));
        (await fixture.ConsumerHarness!.Consumed.Any<StopCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvData_KnownAgvDataUpdatedSuccessFully_AgvDataExist()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        fixture.AgvDataRegistry.Setup(x => x.AddOrUpdateAsync(It.IsAny<AgvData>())).ReturnsAsync(true);
        var agvData = new AgvData(agv.CorrelationId, "model", "type", null, 100d, null, 10d, 10.0d, null, null, null, null, Array.Empty<AgvData.Sensor>());
        await fixture.TestHarness!.Bus.Publish(new UpdateAgvData(agv.CorrelationId, agvData));
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateAgvData>()).ShouldBeTrue();
        agv.DataExist.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ValidData_ScheduledOfflineMessageSavedOfflineToken()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        ScheduledMessage<AgvWentOffline> previousOfflineMessage = await fixture.ScheduleMessage(TimeSpan.FromMinutes(1), new AgvWentOffline(agv.CorrelationId, DateTime.UtcNow));
        agv.SetOnline(previousOfflineMessage.TokenId);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.AgvId, _mapper.Map<AgvStatus>(agv.AgvStatus!));
        await fixture.TestHarness!.Bus.Publish(updateAgvStatus);
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateAgvStatus>()).ShouldBeTrue();
        SendContext<AgvWentOffline> newOfflineMessage = fixture.TestHarness!.Sent.Select<AgvWentOffline>(m => m.Context.Message != previousOfflineMessage.Payload).Select(m => m.Context).Single();
        agv.IsOnline.ShouldBeTrue();
        agv.AgvOfflineTokenId.ShouldBe(newOfflineMessage.ScheduledMessageId);
        newOfflineMessage.Message.CorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_BatteryChargeChanged_PublishesAgvBatteryChargeChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        double newBatteryCharge = agv.AgvStatus!.BatteryCharge - 2.0d * TransportOrder.Domain.Models.Agv.Agv.BatteryChargeChangedThreshold;

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, newBatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.Disturbed, agv.AgvStatus!.LastNodeId);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.AgvId, newAgvStatus);
        await fixture.TestHarness!.Bus.Publish(updateAgvStatus);
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateAgvStatus>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<AgvBatteryChargeChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_LastNodeIdChanged_PublishesAgvLastNodeIdChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.Disturbed, "newLastNodeId");
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.AgvId, newAgvStatus);
        await fixture.TestHarness!.Bus.Publish(updateAgvStatus);
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateAgvStatus>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChanged_PublishesAgvPropertyChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateChargingAgv();
        agv.IsOnline.ShouldBeFalse();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.AgvId, newAgvStatus);
        await fixture.TestHarness!.Bus.Publish(updateAgvStatus);
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateAgvStatus>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChangedForIdleAgv_PublishesAgvPropertyChangedAndAgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsOnline.ShouldBeFalse();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.AgvId, newAgvStatus);
        await fixture.TestHarness!.Bus.Publish(updateAgvStatus);
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateAgvStatus>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ShouldRequestData_PublishesRequestAgvData()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId")
        {
            DataExist = false,
            AgvDataRequestedLastTime = null,
            AgvStatus = new TransportOrder.Domain.Models.Agv.AgvStatus()
        };
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.AgvId, newAgvStatus);
        await fixture.TestHarness!.Bus.Publish(updateAgvStatus);
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateAgvStatus>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<Common.Messages.AgvManagement.Commands.RequestAgvData>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ShouldRequestGraph_PublishesRequestAgvGraph()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId")
        {
            GraphExists = false,
            AgvGraphRequestedLastTime = null,
            AgvStatus = new TransportOrder.Domain.Models.Agv.AgvStatus()
        };
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.AgvId, newAgvStatus);
        await fixture.TestHarness!.Bus.Publish(updateAgvStatus);
        (await fixture.ConsumerHarness!.Consumed.Any<UpdateAgvStatus>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<Common.Messages.AgvManagement.Commands.RequestAgvGraph>()).ShouldBeTrue();
    }


    [TestMethod]
    public async Task Consume_GoOffline_AgvWasOnline_PublishedAgvPropertyChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        ScheduledMessage<AgvWentOffline> offlineMessage = await fixture.ScheduleMessage(TimeSpan.FromMinutes(1), new AgvWentOffline(agv.CorrelationId, DateTime.UtcNow));
        agv.SetOnline(offlineMessage.TokenId);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.TestHarness!.Bus.Publish(new GoOffline(agv.CorrelationId, agv.AgvOfflineTokenId!.Value));
        (await fixture.ConsumerHarness!.Consumed.Any<GoOffline>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
        agv.IsOnline.ShouldBeFalse();
        agv.AgvOfflineTokenId.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_GoOffline_AgvWasOffline_AgvPropertyChangedNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.TestHarness!.Bus.Publish(new GoOffline(agv.CorrelationId, Guid.NewGuid()));
        (await fixture.ConsumerHarness!.Consumed.Any<GoOffline>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeFalse();
        agv.IsOnline.ShouldBeFalse();
        agv.AgvOfflineTokenId.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_LockAgv_ValidData_LocksAgvPublishesAgvPropertyChangedRespondsLockAgvResult()
    {
        var fixture = new AgvCommandHandlerTestFixture(busRegistrationConfigurator: b => b.AddRequestClient<LockAgv>());
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        Response<LockAgvResult> response = await fixture.GetResponseAsync<LockAgvResult, LockAgv>(new LockAgv(agv.CorrelationId));
        response.Message.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        (await fixture.TestHarness!.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_LockAgv_AgvUnknown_RespondsAgvNotFoundResult()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture(busRegistrationConfigurator: b => b.AddRequestClient<LockAgv>());
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, LockAgv>(new LockAgv(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_UnlockAgv_ValidData_UnlocksAgvPublishesAgvPropertyChangedRespondsUnlockResult()
    {
        var fixture = new AgvCommandHandlerTestFixture(busRegistrationConfigurator: b => b.AddRequestClient<UnlockAgv>());
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        Response<UnlockAgvResult> response = await fixture.GetResponseAsync<UnlockAgvResult, UnlockAgv>(new UnlockAgv(agv.CorrelationId));
        response.Message.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        (await fixture.TestHarness!.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
        (await fixture.TestHarness!.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UnlockAgv_AgvUnknown_RespondsAgvNotFoundResult()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture(busRegistrationConfigurator: b => b.AddRequestClient<UnlockAgv>());
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, UnlockAgv>(new UnlockAgv(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_SetAgvForceIdle_ExistingCorrelationId_RespondsWithCorrelationId()
    {
        var fixture = new AgvCommandHandlerTestFixture(busRegistrationConfigurator: b => b.AddRequestClient<SetAgvForceIdle>());
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        Response<SetAgvForceIdleResult> response = await fixture.GetResponseAsync<SetAgvForceIdleResult, SetAgvForceIdle>(new SetAgvForceIdle(agv.CorrelationId));
        response.Message.CorrelationId.ShouldBe(agv.CorrelationId);
        (await fixture.TestHarness!.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_SetAgvForceIdle_AgvUnknown_RespondsAgvNotFoundResult()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture(busRegistrationConfigurator: b => b.AddRequestClient<SetAgvForceIdle>());
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, SetAgvForceIdle>(new SetAgvForceIdle(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_HandleAgvActionStartFailure_ValidData_AgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.TestHarness!.Bus.Publish(new HandleAgvActionStartFailure(agv.CorrelationId));
        (await fixture.ConsumerHarness!.Consumed.Any<HandleAgvActionStartFailure>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_ValidData_AgvPreparedForStartingAction()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.TestHarness!.Bus.Publish(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));
        (await fixture.ConsumerHarness!.Consumed.Any<PrepareAgvForStartingAction>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<AgvPreparedForStartingAction>()).ShouldBeTrue();
    }
}
