using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper.AgvActionGenerator;
using AgvStatus = Gebhardt.StoreWare.Agv.Common.Domain.Models.AgvStatus;
using Cargo = Gebhardt.StoreWare.Agv.Common.Domain.Models.Cargo;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.CommandHandler.Agv;

[TestClass]
public class AgvCommandHandlerTest
{
    private readonly IMapper _mapper = AgvCommandHandlerTestFixture.GetMapper();

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_ValidData_PublishesAbortActionGroup()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateChargingAgv();

        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        fixture.AddAgvs([agv]);
        fixture.WithCurrentAgvAction(actionGroup.Actions.Last());
        fixture.SetupAgv(true);

        await fixture.PublishMessageAsync(new TryReactivateChargingAgv(Guid.NewGuid()));

        (await fixture.TestHarness.Published.Any<AbortActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_NoPendingAction_AbortActionGroupNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateChargingAgv();

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        fixture.SetupAgv(hasPendingActions: false);

        await fixture.PublishMessageAsync(new TryReactivateChargingAgv(Guid.NewGuid()));

        (await fixture.TestHarness.Published.Any<AbortActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_NoChargingAgvs_AbortActionGroupNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>());
        await fixture.PublishMessageAsync(new TryReactivateChargingAgv(Guid.NewGuid()));

        (await fixture.TestHarness.Published.Any<AbortActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AbortChargeAction_AgvWithRunningChargingAction_PublishesAbortActionGroup()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentAgvAction(AgvActionGenerator.GenerateChargeActionGroup().Actions.Last());

        await fixture.PublishMessageAsync(new AbortChargeAction(Guid.NewGuid()));
        (await fixture.TestHarness.Published.Any<AbortActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AbortChargeAction_AgvWithRunningMoveAction_DoesNotPublishAnything()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentAgvAction(AgvActionGenerator.GenerateParkAction());

        await fixture.PublishMessageAsync(new AbortChargeAction(Guid.NewGuid()));
        (await fixture.TestHarness.Published.Any<AbortActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AbortChargeAction_AgvWithNoAction_DoesNotPublishAnything()
    {
        var fixture = new AgvCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new AbortChargeAction(Guid.NewGuid()));
        (await fixture.TestHarness.Published.Any<AbortActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WithErrors_SetHasErrorToTrue()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, [AgvErrorGenerator.GenerateAgvError(errorLevel: ErrorLevel.Fatal)]));
        fixture.AgvDb.Verify(r =>
            r.Agvs.UpdateAgvAsync(It.Is((TransportOrder.Domain.Models.Agv.Agv a) => a.CorrelationId == agv.CorrelationId && a.HasError)), Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WithWarnings_DoesNotSetHasError()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, [AgvErrorGenerator.GenerateAgvError(errorLevel: ErrorLevel.Warning)]));
        fixture.AgvDb.Verify(r =>
            r.Agvs.UpdateAgvAsync(It.Is((TransportOrder.Domain.Models.Agv.Agv a) => a.CorrelationId == agv.CorrelationId && a.HasError)), Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WhenErrorResolveAndIdle_PublishesAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateIdleAgv(hasError: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, []));
        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WhenErrorResolveButBusy_DoesNotPublishAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateBusyAgv(hasError: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, []));
        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvData_KnownAgvDataUpdatedSuccessFully_AgvDataExist()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        fixture.AgvDb.Setup(x => x.Data.AddOrUpdateAsync(It.IsAny<AgvData>()));
        var agvData = AgvDataGenerator.GenerateAgvData();
        await fixture.PublishMessageAsync(new UpdateAgvData(agv.CorrelationId, agvData));
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

        await fixture.PublishMessageAsync(new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, _mapper.Map<AgvStatus>(agv.AgvStatus!)));
        SendContext<AgvWentOffline> newOfflineMessage = fixture.TestHarness.Sent.Select<AgvWentOffline>(m => m.Context.Message != previousOfflineMessage.Payload).Select(m => m.Context).Single();
        agv.IsOnline.ShouldBeTrue();
        agv.AgvOfflineTokenId.ShouldBe(newOfflineMessage.ScheduledMessageId);
        newOfflineMessage.Message.CorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_BatteryChargeChanged_PublishesAgvBatteryChargeChangedUpdatesBatteryChargeGauge()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        double newBatteryCharge = agv.AgvStatus!.BatteryCharge - 2.0d * TransportOrder.Domain.Models.Agv.Agv.BatteryChargeChangedThreshold;

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, newBatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, agv.AgvStatus!.LastNodeId);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<AgvBatteryChargeChanged>()).ShouldBeTrue();
        fixture.VerifyBatteryMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_LastNodeIdChanged_PublishesAgvLastNodeIdChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, "newLastNodeId");
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChanged_PublishesAgvOnlineChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateChargingAgv();
        agv.IsOnline.ShouldBeFalse();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<AgvOnlineChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChangedForIdleAgv_PublishesAgvOnlineChangedAndBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsOnline.ShouldBeFalse();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, false, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<AgvOnlineChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ShouldRequestData_PublishesRequestAgvData()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId", Guid.NewGuid())
        {
            DataExist = false,
            AgvDataRequestedLastTime = null,
            AgvStatus = new TransportOrder.Domain.Models.Agv.AgvStatus()
        };
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ShouldRequestGraph_PublishesRequestAgvGraph()
    {
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId", Guid.NewGuid())
        {
            GraphExists = false,
            AgvGraphRequestedLastTime = null,
            AgvStatus = new TransportOrder.Domain.Models.Agv.AgvStatus()
        };

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithGraphExists(true);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
        fixture.AgvDb.Verify(r =>
            r.Agvs.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_UnknownAgv_PublishesRequestAgvData()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>());
        fixture.SetupAgv(graphExists: true);
        var agvStatus = new AgvStatus(Guid.NewGuid(), null, false, true, 100.0d, false, 48, -10, 20, false);
        var updateAgvStatus = new UpdateAgvStatus(agvStatus.CorrelationId, "serialNumber", agvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);

        (await fixture.TestHarness.Published.Any<AgvDiscovered>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeFalse();
        fixture.AgvDb.Verify(r =>
            r.Agvs.AddAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_UnknownAgvGraphDoesNotExist_PublishesRequestAgvDataRequestAgvGraphAndAgv()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>());
        fixture.SetupAgv(graphExists: false);
        var agvStatus = new AgvStatus(Guid.NewGuid(), null, false, true, 100.0d, false, 48, -10, 20, false);
        var updateAgvStatus = new UpdateAgvStatus(agvStatus.CorrelationId, "serialNumber", agvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);

        (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
        fixture.AgvDb.Verify(r =>
            r.Agvs.AddAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_GoOffline_AgvWasOnline_PublishedAgvOnlineChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        ScheduledMessage<AgvWentOffline> offlineMessage = await fixture.ScheduleMessage(TimeSpan.FromMinutes(1), new AgvWentOffline(agv.CorrelationId, DateTime.UtcNow));
        agv.SetOnline(offlineMessage.TokenId);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new GoOffline(agv.CorrelationId, agv.AgvOfflineTokenId!.Value));

        (await fixture.TestHarness.Published.Any<AgvOnlineChanged>()).ShouldBeTrue();
        agv.IsOnline.ShouldBeFalse();
        agv.AgvOfflineTokenId.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_GoOffline_AgvWasOffline_AgvOnlineChangedNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new GoOffline(agv.CorrelationId, Guid.NewGuid()));

        (await fixture.TestHarness.Published.Any<AgvOnlineChanged>()).ShouldBeFalse();
        agv.IsOnline.ShouldBeFalse();
        agv.AgvOfflineTokenId.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_LockAgv_ValidData_LocksAgvPublishesAgvLockedChangedRespondsLockAgvResult()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        Response<LockAgvResult> response = await fixture.GetResponseAsync<LockAgvResult, LockAgv>(new LockAgv(agv.CorrelationId));
        response.Message.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        (await fixture.TestHarness.Published.Any<AgvLockedChanged>()).ShouldBeTrue();
        var agvLockedChanged = fixture.TestHarness.Published.Select<AgvLockedChanged>().Single().Context.Message;
        agvLockedChanged.IsNowLocked.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_LockAgv_AgvUnknown_RespondsAgvNotFoundResult()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture()
            .WithoutAgv();
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, LockAgv>(new LockAgv(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_UnlockAgv_ValidData_UnlocksAgvPublishesAgvLockedChangedRespondsUnlockResult()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        Response<UnlockAgvResult> response = await fixture.GetResponseAsync<UnlockAgvResult, UnlockAgv>(new UnlockAgv(agv.CorrelationId));
        response.Message.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvLockedChanged>()).ShouldBeTrue();
        var agvLockedChanged = fixture.TestHarness.Published.Select<AgvLockedChanged>().Single().Context.Message;
        agvLockedChanged.IsNowLocked.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_UnlockAgv_AgvUnknown_RespondsAgvNotFoundResult()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture()
            .WithoutAgv();
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, UnlockAgv>(new UnlockAgv(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_SetAgvForceIdle_ExistingCorrelationId_RespondsWithCorrelationId()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        Response<SetAgvForceIdleResult> response = await fixture.GetResponseAsync<SetAgvForceIdleResult, SetAgvForceIdle>(new SetAgvForceIdle(agv.CorrelationId));
        response.Message.CorrelationId.ShouldBe(agv.CorrelationId);
        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_SetAgvForceIdle_AgvUnknown_RespondsAgvNotFoundResult()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture()
            .WithoutAgv();
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, SetAgvForceIdle>(new SetAgvForceIdle(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_HandleAgvActionStartFailure_ValidData_AgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasStartedAction(false);
        var agv = AgvGenerator.GenerateBusyAgv();
        fixture.AddAgvs([agv]);

        await fixture.PublishMessageAsync(new HandleAgvActionStartFailure(agv.CorrelationId));

        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_HandleAgvActionStartFailure_AgvHasStartedAction_DoesNotPublishAgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasStartedAction(true);
        var agv = AgvGenerator.GenerateBusyAgv();
        fixture.AddAgvs([agv]);
        await fixture.PublishMessageAsync(new HandleAgvActionStartFailure(agv.CorrelationId));

        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_ValidData_AgvPreparedForStartingAction()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: NodeGenerator.GenerateParkingNode().Id);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        (await fixture.TestHarness.Published.Any<AgvPreparedForStartingAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AgvStatusNotChanged_NotPublishedAgvStatusChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetOnline(Guid.NewGuid());
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var currentAgStatus = agv.AgvStatus!;
        var agvStatus = new AgvStatus(agv.CorrelationId, currentAgStatus.OrderId, currentAgStatus.Busy, currentAgStatus.Automatic, currentAgStatus.BatteryCharge, currentAgStatus.Charging, 48, currentAgStatus.Charging ? 20 : -10, 20, currentAgStatus.Disturbed, currentAgStatus.LastNodeId);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, agvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);

        (await fixture.TestHarness.Published.Any<AgvBatteryChargeChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AgvOnlineChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_UpdateCargo_CargoChanged_PublishesHasCargoChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agv.CorrelationId);
        var domainCargos = _mapper.Map<IReadOnlyCollection<TransportOrder.Domain.Models.Agv.Cargo>>(cargos);

        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasCargoStates([true, false])
            .WithSetCargoReturnValue(agv.CorrelationId, domainCargos, true);
        fixture.AddAgvs([agv]);

        var updateCargo = new UpdateCargo(agv.CorrelationId, cargos.ToList().AsReadOnly());
        await fixture.PublishMessageAsync(updateCargo);

        (await fixture.TestHarness.Published.Any<CargoChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HasCargoChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateCargo_CargoUnchanged_DoesNotPublishHasCargoChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agv.CorrelationId);
        var domainCargos = _mapper.Map<IReadOnlyCollection<TransportOrder.Domain.Models.Agv.Cargo>>(cargos);

        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasCargoStates([true, true])
            .WithSetCargoReturnValue(agv.CorrelationId, domainCargos, false);
        fixture.AddAgvs([agv]);

        var updateCargo = new UpdateCargo(agv.CorrelationId, cargos.ToList().AsReadOnly());
        await fixture.PublishMessageAsync(updateCargo);

        (await fixture.TestHarness.Published.Any<CargoChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<HasCargoChanged>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_ValidData_PublishesParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .AddParkingPointManager(parkGraphNode: new GraphNode(parkNodeName));

        await fixture.PublishMessageAsync(new PrepareParkingOfAgv(agv.CorrelationId));
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ParkingNodeSelectedForAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_BatteryIsLow_DoesNotPublishParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 20);
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .AddParkingPointManager(parkGraphNode: new GraphNode(parkNodeName));

        var command = new PrepareParkingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<PrepareParkingOfAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ParkingNodeSelectedForAgv>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_AgvIsLocked_DoesNotPublishParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.Lock();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .AddActionGroupRetrieval(headingTo: false)
            .AddParkingPointManager(parkGraphNode: new GraphNode(parkNodeName));

        var command = new PrepareParkingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<PrepareParkingOfAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ParkingNodeSelectedForAgv>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_HasAlreadyReservedParkingNode_DoesNotPublishParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(true)
            .AddParkingPointManager(parkGraphNode: new GraphNode(parkNodeName));

        var command = new PrepareParkingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        (await fixture.ConsumerHarness.Consumed.Any<PrepareParkingOfAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ParkingNodeSelectedForAgv>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_ValidData_PublishesChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string chargeNodeName = "Charger-1";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([])
            .AddChargingPointManager(new GraphNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);

        (await fixture.ConsumerHarness.Consumed.Any<PrepareChargingOfAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ChargingNodeSelectedForAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_AgvIsLocked_DoesNotPublishChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.Lock();
        const string chargeNodeName = "Charger-1";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([])
            .AddChargingPointManager(new GraphNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);

        (await fixture.ConsumerHarness.Consumed.Any<PrepareChargingOfAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ChargingNodeSelectedForAgv>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_AgvIsCharging_DoesNotPublishChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        const string chargeNodeName = "Charger-1";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([])
            .AddChargingPointManager(new GraphNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);

        (await fixture.ConsumerHarness.Consumed.Any<PrepareChargingOfAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ChargingNodeSelectedForAgv>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_HasAlreadyAReservedCharger_DoesNotPublishChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string chargeNodeName = "Charger-1";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(true)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([])
            .AddChargingPointManager(new GraphNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);

        (await fixture.ConsumerHarness.Consumed.Any<PrepareChargingOfAgv>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<ChargingNodeSelectedForAgv>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvSettingsGroupReference_ValidData_AgvSettingsGroupPropertyUpdated()
    {
        var previousGroupId = Guid.Empty;
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.AgvSettingsGroup = previousGroupId;
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);
        var command = new UpdateAgvSettingsGroupReference(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);

        (await fixture.ConsumerHarness.Consumed.Any<UpdateAgvSettingsGroupReference>()).ShouldBeTrue();
        agv.AgvSettingsGroup.ShouldNotBe(previousGroupId);
    }
}