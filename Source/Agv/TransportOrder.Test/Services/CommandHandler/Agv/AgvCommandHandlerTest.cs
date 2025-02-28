using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using AgvStatus = Gebhardt.Acc.Common.Domain.Models.AgvStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using RequestAgvData = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvGraph;
using UnlockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.UnlockAgv;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Agv;

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

        await fixture.ShouldHavePublishedAsync<AbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_NoPendingAction_AbortActionGroupNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateChargingAgv();

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        fixture.SetupAgv(hasPendingActions: false);

        await fixture.PublishMessageAsync(new TryReactivateChargingAgv(Guid.NewGuid()));

        await fixture.ShouldNotHavePublishedAsync<AbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_NoChargingAgvs_AbortActionGroupNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>());
        await fixture.PublishMessageAsync(new TryReactivateChargingAgv(Guid.NewGuid()));
        
        await fixture.ShouldNotHavePublishedAsync<AbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AbortChargeAction_AgvWithRunningChargingAction_PublishesAbortActionGroup()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentAgvAction(AgvActionGenerator.GenerateChargeActionGroup().Actions.Last());

        await fixture.PublishMessageAsync(new AbortChargeAction(Guid.NewGuid()));
        await fixture.ShouldHavePublishedAsync<AbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AbortChargeAction_AgvWithRunningMoveAction_DoesNotPublishAnything()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentAgvAction(AgvActionGenerator.GenerateParkAction());

        await fixture.PublishMessageAsync(new AbortChargeAction(Guid.NewGuid()));
        await fixture.ShouldNotHavePublishedAsync<AbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_AbortChargeAction_AgvWithNoAction_DoesNotPublishAnything()
    {
        var fixture = new AgvCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new AbortChargeAction(Guid.NewGuid()));
        await fixture.ShouldNotHavePublishedAsync<AbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WithErrors_SetHasErrorToTrue()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, errorLevel: ErrorLevel.Fatal);
        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, errorStatus.Errors!, errorStatus.AgvPosition));
        fixture.AgvDb.Verify(r =>
            r.Agvs.UpdateAgvAsync(It.Is((TransportOrder.Domain.Models.Agv.Agv a) => a.CorrelationId == agv.CorrelationId && a.HasError)), Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WithWarnings_DoesNotSetHasError()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, errorLevel: ErrorLevel.Warning);
        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, errorStatus.Errors!, errorStatus.AgvPosition));
        fixture.AgvDb.Verify(r =>
            r.Agvs.UpdateAgvAsync(It.Is((TransportOrder.Domain.Models.Agv.Agv a) => a.CorrelationId == agv.CorrelationId && a.HasError)), Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WhenErrorResolveAndIdle_PublishesAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateIdleAgv(hasError: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, [], new AgvPosition(true, 1,1,1, 0)));
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WhenErrorResolveButBusy_DoesNotPublishAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateBusyAgv(hasError: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, [], new AgvPosition(true, 1, 1, 1, 0)));
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
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
    public async Task Consume_UpdateAgvStatus_AgvWasOffline_SetOnline()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasOnlineAgvs()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, _mapper.Map<AgvStatus>(agv.AgvStatus!)));
        agv.IsOnline.ShouldBeTrue();
        await fixture.ShouldHavePublishedAsync<AgvWentOnline>();
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
        await fixture.ShouldHavePublishedAsync<AgvBatteryChargeChanged>();
        fixture.VerifyBatteryMetricsCollected(Times.Once());
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_LastNodeIdChanged_NewLastNodeIdIsOnGraph_PublishesAgvLastNodeIdChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, "newLastNodeId");

        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true)
            .WithNode(new GraphNode(newAgvStatus.LastNodeId!));
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_LastNodeIdChanged_NewLastNodeIdIsNotOnGraph_DoesNotPublishAgvLastNodeIdChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, "newLastNodeId");

        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true)
            .WithNode(null);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldNotHavePublishedAsync<AgvLastNodeIdChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChanged_PublishesAgvOnlineChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateChargingAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChangedForIdleAgv_PublishesAgvOnlineChangedAndBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, false, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ShouldRequestData_PublishesRequestAgvData()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateRawAgv();
        agv.DataExist = false;
        agv.AgvDataRequestedLastTime = null;
        agv.AgvStatus = new TransportOrder.Domain.Models.Agv.AgvStatus();
        
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<RequestAgvData>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ShouldRequestGraph_PublishesRequestAgvGraph()
    {
        var agv = AgvGenerator.GenerateRawAgv();
        agv.GraphExists = false;
        agv.AgvDataRequestedLastTime = null;
        agv.AgvStatus = new TransportOrder.Domain.Models.Agv.AgvStatus();
        
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithGraphExists(true);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<RequestAgvGraph>();
        fixture.AgvDb.Verify(r =>
            r.Agvs.UpdateAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_UnknownAgv_PublishesRequestAgvData()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>());
        fixture.SetupAgv(exists: true);
        var agvStatus = new AgvStatus(Guid.NewGuid(), null, false, true, 100.0d, false, 48, -10, 20, false);
        var updateAgvStatus = new UpdateAgvStatus(agvStatus.CorrelationId, "serialNumber", agvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        
        await fixture.ShouldHavePublishedAsync<AgvDiscovered>();
        await fixture.ShouldHavePublishedAsync<RequestAgvData>();
        await fixture.ShouldNotHavePublishedAsync<RequestAgvGraph>();
        fixture.AgvDb.Verify(r =>
            r.Agvs.AddAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_UnknownAgvGraphDoesNotExist_PublishesRequestAgvDataRequestAgvGraphAndAgv()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>());
        fixture.SetupAgv(exists: false);
        var agvStatus = new AgvStatus(Guid.NewGuid(), null, false, true, 100.0d, false, 48, -10, 20, false);
        var updateAgvStatus = new UpdateAgvStatus(agvStatus.CorrelationId, "serialNumber", agvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        
        await fixture.ShouldHavePublishedAsync<RequestAgvData>();
        await fixture.ShouldHavePublishedAsync<RequestAgvGraph>();
        fixture.AgvDb.Verify(r =>
            r.Agvs.AddAgvAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_GoOffline_AgvWasOnline_PublishedAgvOnlineChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetOnline();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv }).WithHasOnlineAgvs(false);

        await fixture.PublishMessageAsync(new GoOffline(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        agv.IsOnline.ShouldBeFalse();
        await fixture.ShouldHavePublishedAsync<AllAgvsOffline>();
    }

    [TestMethod]
    public async Task Consume_GoOffline_AgvWasOffline_AgvOnlineChangedNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new GoOffline(agv.CorrelationId));

        (await fixture.TestHarness.Published.Any<AgvOnlineChanged>()).ShouldBeFalse();
        agv.IsOnline.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_LockAgv_ValidData_PublishesAgvModeChangePendingRespondsLockAgvResult()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        
        Response<LockAgvResult> response = await fixture.GetResponseAsync<LockAgvResult, LockAgv>(
            new LockAgv(agv.CorrelationId, LockReason.Other, "reason description", "agv", AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, false));
        
        response.Message.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<AgvModeChangePending>();
    }

    [TestMethod]
    public async Task Consume_LockAgv_AgvUnknown_RespondsAgvNotFoundResult()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture()
            .WithoutAgv();
        Response<AgvNotFoundResult> response = await fixture.GetResponseAsync<AgvNotFoundResult, LockAgv>(
            new LockAgv(agvCorrelationId, LockReason.Other, "reason", "agv", AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.Pause, false));
        response.Message.CorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_UnlockAgv_ValidData_UnlocksAgvPublishesAgvLockedChangedRespondsUnlockResult()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        Response<UnlockAgvResult> response = await fixture.GetResponseAsync<UnlockAgvResult, UnlockAgv>(new UnlockAgv(agv.CorrelationId));
        response.Message.AgvCorrelationId.ShouldBe(agv.CorrelationId);
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldHavePublishedAsync<AgvLockedChanged>();
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
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
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
        
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_HandleAgvActionStartFailure_AgvHasStartedAction_DoesNotPublishAgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasStartedAction(true);
        var agv = AgvGenerator.GenerateBusyAgv();
        fixture.AddAgvs([agv]);
        await fixture.PublishMessageAsync(new HandleAgvActionStartFailure(agv.CorrelationId));
        
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_NotQueueing_AgvPreparedForStartingAction()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: NodeGenerator.GenerateParkingNode().Id);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithAgvsToMove(null)
            .WithActionGroup(actionGroup);
        
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryReactivateParkingAgvs>();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_WasQueuingAtQueueHead_AutomaticPickDropAction_CreateQueueingActionGroup()
    {
        var agvsToMove = new Dictionary<Guid, string> { { Guid.NewGuid(), "a queue head" } };
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "queue node");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithAgvsToMove(agvsToMove)
            .WithActionGroup(actionGroup);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_WasQueuingAtQueueHeadNoOtherAgvsInQueue_AutomaticPickDropAction_QueueBecameEmpty()
    {
        var agvsToMove = new Dictionary<Guid, string>();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "queue node");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithAgvsToMove(agvsToMove)
            .WithActionGroup(actionGroup);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldHavePublishedAsync<QueueBecameEmpty>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_NotQueueing_ChargeAction_DoesNotPublishCreateQueueingActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "a node");
        agv.IsQueueing = false;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv:agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsToMove(null)
            .WithActionGroup(actionGroup);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<TryReactivateParkingAgvs>();
        agv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_IsQueuing_ManualPickDropAction_CreateQueueingActionGroup()
    {
        var agvsToMove = new Dictionary<Guid, string> { { Guid.NewGuid(), "queue node" } };
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "queue node");
        agv.IsQueueing = true;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.IsManual = true;
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsToMove(agvsToMove)
            .WithActionGroup(actionGroup)
            .WithIsOnAQueueNode(true);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        agv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_IsQueuing_ChargeAction_CreateQueueingActionGroup()
    {
        var agvsToMove = new Dictionary<Guid, string> { { Guid.NewGuid(), "queue node" } };
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "queue node");
        agv.IsQueueing = true;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsToMove(agvsToMove)
            .WithActionGroup(actionGroup)
            .WithIsOnAQueueNode(true);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        agv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_IsQueuing_ChargeAction_NoOtherAgvsInQueue_QueueBecameEmpty()
    {
        var agvsToMove = new Dictionary<Guid, string>();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "queue node");
        agv.IsQueueing = true;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsToMove(agvsToMove, 0)
            .WithActionGroup(actionGroup)
            .WithIsOnAQueueNode(true);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<QueueBecameEmpty>();
        agv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_AgvIsLocked_AgvPreparedForStartingActionNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: NodeGenerator.GenerateParkingNode().Id, mode: new AgvMode(ModeType.Locked));
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<AgvPreparedForStartingAction>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AgvStatusNotChanged_NotPublishedAgvStatusChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.SetOnline();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var currentAgStatus = agv.AgvStatus!;
        var agvStatus = new AgvStatus(agv.CorrelationId, currentAgStatus.OrderId, currentAgStatus.Busy, currentAgStatus.Automatic, currentAgStatus.BatteryCharge, currentAgStatus.Charging, 48, currentAgStatus.Charging ? 20 : -10, 20, currentAgStatus.Disturbed, currentAgStatus.LastNodeId);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, agvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        
        await fixture.ShouldNotHavePublishedAsync<AgvBatteryChargeChanged>();
        await fixture.ShouldNotHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<AgvOnlineChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateCargo_CargoChanged_PublishesHasCargoChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agv.CorrelationId);
        var domainCargos = _mapper.Map<IReadOnlyCollection<TransportOrder.Domain.Models.Agv.Cargo>>(cargos);

        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasCargoStates(true, false)
            .WithSetCargoReturnValue(agv.CorrelationId, domainCargos, true);
        fixture.AddAgvs([agv]);

        var updateCargo = new UpdateCargo(agv.CorrelationId, cargos.ToList().AsReadOnly());
        await fixture.PublishMessageAsync(updateCargo);
        
        await fixture.ShouldHavePublishedAsync<CargoChanged>();
        await fixture.ShouldHavePublishedAsync<HasCargoChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateCargo_CargoUnchanged_DoesNotPublishHasCargoChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agv.CorrelationId);
        var domainCargos = _mapper.Map<IReadOnlyCollection<TransportOrder.Domain.Models.Agv.Cargo>>(cargos);

        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasCargoStates(true, true)
            .WithSetCargoReturnValue(agv.CorrelationId, domainCargos, false);
        fixture.AddAgvs([agv]);

        var updateCargo = new UpdateCargo(agv.CorrelationId, cargos.ToList().AsReadOnly());
        await fixture.PublishMessageAsync(updateCargo);
        
        await fixture.ShouldNotHavePublishedAsync<CargoChanged>();
        await fixture.ShouldNotHavePublishedAsync<HasCargoChanged>();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_ValidData_PublishesParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .HasAlreadyReservedNode(false)
            .WithCurrentAssignedActionGroup(false)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName));

        await fixture.PublishMessageAsync(new PrepareParkingOfAgv(agv.CorrelationId));
        
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_BatteryIsLow_DoesNotPublishParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 20);
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .HasAlreadyReservedNode(false)
            .WithCurrentAssignedActionGroup(true)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName));

        var command = new PrepareParkingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<PrepareParkingOfAgv>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_AgvIsLocked_DoesNotPublishParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .AddActionGroupRetrieval(headingTo: false)
            .WithCurrentAssignedActionGroup(false)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName));

        var command = new PrepareParkingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<PrepareParkingOfAgv>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_HasAlreadyReservedParkingNode_DoesNotPublishParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .HasAlreadyReservedNode(true)
            .WithCurrentAssignedActionGroup(false)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName));

        var command = new PrepareParkingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<PrepareParkingOfAgv>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_HasAssignedActionGroup_DoesNotPublishParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .HasAlreadyReservedNode(true)
            .WithCurrentAssignedActionGroup(true)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName));

        var command = new PrepareParkingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<PrepareParkingOfAgv>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareParkingOfAgv_NoParkingNodes_PublishesParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph(withParkingNodes: false)
            .HasAlreadyReservedNode(false)
            .WithCurrentAssignedActionGroup(false)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName));

        await fixture.PublishMessageAsync(new PrepareParkingOfAgv(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
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
            .AddChargingPointManager(NodeGenerator.GenerateChargingNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<PrepareChargingOfAgv>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_AgvIsLocked_DoesNotPublishChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        const string chargeNodeName = "Charger-1";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([])
            .AddChargingPointManager(NodeGenerator.GenerateChargingNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<PrepareChargingOfAgv>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
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
            .AddChargingPointManager(NodeGenerator.GenerateChargingNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<PrepareChargingOfAgv>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_HasChargingActionGroupAlready_DoesNotPublishChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string chargeNodeName = "Charger-1";
        var chargingAction = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv:agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([chargingAction])
            .AddChargingPointManager(NodeGenerator.GenerateChargingNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<PrepareChargingOfAgv>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_HasAlreadyAReservedCharger_PublishChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string chargeNodeName = "Charger-1";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(true)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([])
            .AddChargingPointManager(NodeGenerator.GenerateChargingNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<PrepareChargingOfAgv>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
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
        
        await fixture.ShouldHaveConsumedAsync<UpdateAgvSettingsGroupReference>();
        agv.AgvSettingsGroup.ShouldNotBe(previousGroupId);
    }

    [TestMethod]
    public async Task Consume_SetUserMode_ValidData_PublishesAgvModeChangePending()
    {
        const string initiatingUser = "user";

        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);
        var command = new SetUserMode(agv.CorrelationId, initiatingUser, AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.FinishActionGroup, false);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<SetUserMode>();
        await fixture.ShouldHavePublishedAsync<AgvModeChangePending>();
        agv.Mode.PendingType.ShouldBe(ModeType.UserMode);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
    }

    [TestMethod]
    public async Task Consume_SetMaintenanceMode_ValidData_PublishesAgvModeChangePending()
    {
        const string initiatingUser = "user";
        const MaintenanceReason maintenanceReason = MaintenanceReason.Other;
        const string reasonDescription = "reason description";

        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);
        var command = new SetMaintenanceMode(agv.CorrelationId, initiatingUser, maintenanceReason, reasonDescription, AssignedActionGroupPolicy.Reset, CurrentActionGroupPolicy.FinishActionGroup, false);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<SetMaintenanceMode>();
        await fixture.ShouldHavePublishedAsync<AgvModeChangePending>();
        agv.Mode.PendingType.ShouldBe(ModeType.Maintenance);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
        agv.Mode.MaintenanceReason.ShouldBe(maintenanceReason);
    }

    [TestMethod]
    public async Task Consume_SetNormalModeWithInitiatingUser_ValidData_PublishesAgvModeChanged()
    {
        const string initiatingUser = "user";

        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Maintenance));
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);
        var command = new SetNormalMode(agv.CorrelationId, initiatingUser);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<SetNormalMode>();
        await fixture.ShouldHavePublishedAsync<AgvModeChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
    }

    [TestMethod]
    public async Task Consume_SetNormalMode_ValidData_PublishesAgvModeChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Maintenance));
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);
        var command = new SetNormalMode(agv.CorrelationId, null);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<SetNormalMode>();
        await fixture.ShouldHavePublishedAsync<AgvModeChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.InitiatingUser.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_SetNormalMode_AgvIsLocked_PublishesAgvModeChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);
        var command = new SetNormalMode(agv.CorrelationId, null);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<SetNormalMode>();
        await fixture.ShouldHavePublishedAsync<AgvModeChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.IsLocked.ShouldBeFalse();
        agv.Mode.InitiatingUser.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_TryReactivateParkingAgvs_NoOccupiedParkers_DoesNotPublishAgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([])
            .WithClosestAgv(null);

        await fixture.TestHarness.Bus.Publish(new TryReactivateParkingAgvs());

        await fixture.ShouldHaveConsumedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldNotHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_TryReactivateParkingAgvs_OccupiedParkers_PublishesAgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithAgvsAtQueueHead([])
            .AddAgvs([AgvGenerator.GenerateIdleAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow)])
            .WithClosestAgv(null);
     
        await fixture.TestHarness.Bus.Publish(new TryReactivateParkingAgvs());

        await fixture.ShouldHaveConsumedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_TryReactivateParkingAgvs_AgvsAtQueueHeads_PublishesAgvBecameIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithAgvsAtQueueHead([Guid.NewGuid()])
            .AddAgvs([])
            .WithClosestAgv(null); 

        await fixture.TestHarness.Bus.Publish(new TryReactivateParkingAgvs());

        await fixture.ShouldHaveConsumedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }

    [DataRow(ModeType.Normal, ModeType.Maintenance)]
    [DataRow(ModeType.Normal, ModeType.UserMode)]
    [DataRow(ModeType.Normal, ModeType.Locked)]
    [DataRow(ModeType.Maintenance, ModeType.Locked)]
    [DataTestMethod]
    public async Task Consume_SwitchMode_ValidData_PublishesAgvModeChanged(ModeType previousType, ModeType pendingType)
    {
        var mode = new AgvMode(previousType, Guid.NewGuid());
        mode.PrepareSwitch(pendingType);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode );
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);

        await fixture.TestHarness.Bus.Publish(new SwitchMode(agv.CorrelationId));

        await fixture.ShouldHaveConsumedAsync<SwitchMode>();
        await fixture.ShouldHavePublishedAsync<AgvModeChanged>();
        agv.Mode.Type.ShouldBe(pendingType);
        agv.Mode.PendingType.ShouldBeNull();
        if (pendingType == ModeType.Locked)
        {
            await fixture.ShouldHavePublishedAsync<AgvLockedChanged>();
        }
    }

    [TestMethod]
    public async Task Consume_TryEnqueueAgv_EnqueueingFails_DoesNotPublishCreateQueueingActionGroup()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithEnqueueingTarget(null);

        await fixture.TestHarness.Bus.Publish(new TryEnqueueAgv(Guid.NewGuid(), "a queue node"));
        await fixture.ShouldHaveConsumedAsync<TryEnqueueAgv>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<QueueEntered>();
    }

    [TestMethod]
    public async Task Consume_TryEnqueueAgv_AgvEnqueuesAtQueueEntry_DoesNotPublishCreateQueueingActionGroup()
    {
        const string entryNode = "entry node";
        var fixture = new AgvCommandHandlerTestFixture()
            .WithEnqueueingTarget(entryNode);

        await fixture.TestHarness.Bus.Publish(new TryEnqueueAgv(Guid.NewGuid(), entryNode));
        await fixture.ShouldHaveConsumedAsync<TryEnqueueAgv>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<QueueEntered>();
    }

    [TestMethod]
    public async Task Consume_TryEnqueueAgv_ValidData_PublishesCreateQueueingActionGroup()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithEnqueueingTarget("target queue node");

        await fixture.TestHarness.Bus.Publish(new TryEnqueueAgv(Guid.NewGuid(), "queue entry node"));
        await fixture.ShouldHaveConsumedAsync<TryEnqueueAgv>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<QueueEntered>();
    }
}