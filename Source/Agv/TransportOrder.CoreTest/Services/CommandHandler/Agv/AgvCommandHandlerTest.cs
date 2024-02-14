using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper.AgvActionGenerator;
using AgvStatus = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvStatus;
using Cargo = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Cargo;
using LockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands.RequestAgvData;
using RequestAgvGraph = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands.RequestAgvGraph;
using UnlockAgv = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

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
        var fixture = new AgvCommandHandlerTestFixture().WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        double newBatteryCharge = agv.AgvStatus!.BatteryCharge - 2.0d * TransportOrder.Domain.Models.Agv.Agv.BatteryChargeChangedThreshold;

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, newBatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, agv.AgvStatus!.LastNodeId);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<AgvBatteryChargeChanged>()).ShouldBeTrue();
        MetricCollector.BatteryChargeGauge.WithLabels(agv.CorrelationId.ToString()).Value.ShouldBe(agv.AgvStatus!.BatteryCharge);
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_LastNodeIdChanged_PublishesAgvLastNodeIdChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture().WithGraphExists(true);
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
        var fixture = new AgvCommandHandlerTestFixture().WithGraphExists(true);
        var agv = AgvGenerator.GenerateChargingAgv();
        agv.IsOnline.ShouldBeFalse();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<AgvOnlineChanged>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChangedForIdleAgv_PublishesAgvOnlineChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture().WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsOnline.ShouldBeFalse();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, false, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        (await fixture.TestHarness.Published.Any<AgvOnlineChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ShouldRequestData_PublishesRequestAgvData()
    {
        var fixture = new AgvCommandHandlerTestFixture().WithGraphExists(true);
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId")
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
        var agv = new TransportOrder.Domain.Models.Agv.Agv("agvId")
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
        var fixture = new AgvCommandHandlerTestFixture(b => b.AddRequestClient<LockAgv>());
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
        var fixture = new AgvCommandHandlerTestFixture(b => b.AddRequestClient<LockAgv>()).WithoutAgv();
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, LockAgv>(new LockAgv(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_UnlockAgv_ValidData_UnlocksAgvPublishesAgvLockedChangedRespondsUnlockResult()
    {
        var fixture = new AgvCommandHandlerTestFixture(b => b.AddRequestClient<UnlockAgv>());
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
        var fixture = new AgvCommandHandlerTestFixture(b => b.AddRequestClient<UnlockAgv>()).WithoutAgv();
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, UnlockAgv>(new UnlockAgv(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_SetAgvForceIdle_ExistingCorrelationId_RespondsWithCorrelationId()
    {
        var fixture = new AgvCommandHandlerTestFixture(b => b.AddRequestClient<SetAgvForceIdle>());
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
        var fixture = new AgvCommandHandlerTestFixture(b => b.AddRequestClient<SetAgvForceIdle>()).WithoutAgv();
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, SetAgvForceIdle>(new SetAgvForceIdle(agvCorrelationId));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_HandleAgvActionStartFailure_ValidData_AgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture().WithHasStartedAction(false);
        var agv = AgvGenerator.GenerateBusyAgv();
        fixture.AddAgvs([agv]);

        await fixture.PublishMessageAsync(new HandleAgvActionStartFailure(agv.CorrelationId));

        (await fixture.TestHarness.Published.Any<AgvBecameIdle>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_HandleAgvActionStartFailure_AgvHasStartedAction_DoesNotPublishAgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture().WithHasStartedAction(true);
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
    public async Task Consume_MoveAgv_AgvDoesNotExist_RespondsAgvNotFoundResult()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([]);

        await fixture.PublishMessageAsync(new MoveAgv("unknown agv".ToGuid(), "known target", ActionGroupCreator.TransportOrderWebApi));
        
        (await fixture.TestHarness.Published.Any<AgvNotFoundResult>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvMoveFailed>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AgvMoveSuccessful>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_MoveAgv_TargetDoesNotExist_RespondsAgvMoveFailed()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithNodeExists(false);

        await fixture.PublishMessageAsync(new MoveAgv(agv.CorrelationId, "unknown target", ActionGroupCreator.TransportOrderWebApi));

        (await fixture.TestHarness.Published.Any<AgvNotFoundResult>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AgvMoveFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AgvMoveSuccessful>()).ShouldBeFalse();
    }

    [UsedImplicitly]
    private class FakeQueueActionGroupConsumer: IConsumer<QueueActionGroup>
    {
        public async Task Consume(ConsumeContext<QueueActionGroup> context)
        {
            await context.RespondAsync(new Success());
        }
    }

    [TestMethod]
    public async Task Consume_MoveAgv_ValidData_Responds_AgvMoveSuccessful()
    {    
       var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture(
                busRegistrationConfigurator: c=>
                {
                    c.AddConsumer<FakeQueueActionGroupConsumer>();
                    c.AddRequestClient<QueueActionGroup>();
                })
            .AddAgvs([agv])
            .WithNodeExists(true);

        await fixture.PublishMessageAsync(new MoveAgv(agv.CorrelationId, "known target", ActionGroupCreator.TransportOrderWebApi));
       
        (await fixture.TestHarness.Published.Any<AgvNotFoundResult>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<AgvMoveFailed>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvMoveSuccessful>()).ShouldBeTrue();
    }
}