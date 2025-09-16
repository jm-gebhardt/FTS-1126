using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Zones;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Moq;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using AgvStatus = Gebhardt.Acc.Common.Domain.Models.AgvStatus;
using Cargo = Gebhardt.Acc.Common.Domain.Models.Cargo;
using LockAgv = Gebhardt.Acc.TransportOrder.Messages.Commands.Agv.LockAgv;
using QueueActionGroup = Gebhardt.Acc.Common.Messages.ActionGroups.Commands.QueueActionGroup;
using RequestAgvData = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvData;
using RequestAgvGraph = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvGraph;
using RequestAgvZones = Gebhardt.Acc.Common.Messages.AgvManagement.Commands.RequestAgvZones;
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
    public async Task Consume_AgvErrorChanged_WithErrors_SetHasErrorToTrue()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, errorLevel: ErrorLevel.Fatal);
        await fixture.PublishMessageAsync(new AgvErrorChanged(agv.CorrelationId, errorStatus.Errors));
        fixture.AgvDb.Verify(r =>
            r.Agvs.UpdateAgvAsync(It.Is((TransportOrder.Domain.Models.Agv.Agv a) => a.CorrelationId == agv.CorrelationId && a.HasError)), Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvErrorChanged_WithWarnings_DoesNotSetHasError()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, errorLevel: ErrorLevel.Warning);
        await fixture.PublishMessageAsync(new AgvErrorChanged(agv.CorrelationId, errorStatus.Errors));
        fixture.AgvDb.Verify(r =>
            r.Agvs.UpdateAgvAsync(It.Is((TransportOrder.Domain.Models.Agv.Agv a) => a.CorrelationId == agv.CorrelationId && a.HasError)), Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvErrorChanged_WhenErrorResolveAndIdle_PublishesAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateIdleAgv(hasError: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.PublishMessageAsync(new AgvErrorChanged(agv.CorrelationId, []));
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
    }

    [TestMethod]
    public async Task Consume_AgvErrorChanged_WhenErrorResolveButBusy_DoesNotPublishAgvBecameIdle()
    {
        var agv = AgvGenerator.GenerateBusyAgv(hasError: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        await fixture.PublishMessageAsync(new AgvErrorChanged(agv.CorrelationId, []));
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
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .WithHasOnlineAgvs()
            .WithZones([])
            .WithCurrentStartedOrCreatedActionGroups([])
            .WithGraphExists(true);
        agv.GoOffline();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });
        agv.AgvStatus!.UpdateLastNodeId(Guid.Empty);
        await fixture.PublishMessageAsync(new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, _mapper.Map<AgvStatus>(agv.AgvStatus!)));
        agv.IsOnline.ShouldBeTrue();
        await fixture.ShouldHavePublishedAsync<AgvWentOnline>();
        await fixture.ShouldHavePublishedAsync<CheckAgvLoad>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_BatteryStatusChangedUnderThreshold_DoNotPublishesAgvBatteryChargeChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        double newBatteryCharge = agv.AgvStatus!.BatteryCharge - TransportOrder.Domain.Models.Agv.Agv.BatteryChargeDischargingThreshold;
        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, newBatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.BatteryVoltage, agv.AgvStatus!.BatteryCurrent, agv.AgvStatus!.BatteryTemperature, agv.AgvStatus!.Disturbed, agv.AgvStatus!.LastNodeId, agv.AgvStatus!.EstimatedFullyChargedTime, agv.AgvStatus!.LastBatteryReplacement);

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldNotHavePublishedAsync<AgvBatteryStatusChanged>();
        await fixture.ShouldNotHavePublishedAsync<AgvBatteryChargeChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_BatteryChargeChanged_PublishesAgvBatteryStatusChangedUpdatesBatteryChargeGauge()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs([agv]);

        double newBatteryCharge = agv.AgvStatus!.BatteryCharge - TransportOrder.Domain.Models.Agv.Agv.BatteryChargeDischargingThreshold * 1.01;
        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, newBatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.BatteryVoltage, agv.AgvStatus!.BatteryCurrent, agv.AgvStatus!.BatteryTemperature, agv.AgvStatus!.Disturbed, agv.AgvStatus!.LastNodeId, agv.AgvStatus!.EstimatedFullyChargedTime, agv.AgvStatus!.LastBatteryReplacement);

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvBatteryStatusChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBatteryChargeChanged>();
        fixture.VerifyBatteryMetricsCollected(Times.Once());
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_BatteryTemperatureChanges_DoesNotPublishesAgvBatteryChargeChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs([agv]);

        double newBatteryCharge = agv.AgvStatus!.BatteryCharge - TransportOrder.Domain.Models.Agv.Agv.BatteryChargeDischargingThreshold * 0.99;
        double newTemperature = agv.AgvStatus!.BatteryTemperature + TransportOrder.Domain.Models.Agv.Agv.BatteryTemperatureThreshold * 1.01;
        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, newBatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.BatteryVoltage, agv.AgvStatus!.BatteryCurrent, newTemperature, agv.AgvStatus!.Disturbed, agv.AgvStatus!.LastNodeId, agv.AgvStatus!.EstimatedFullyChargedTime, agv.AgvStatus!.LastBatteryReplacement);

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvBatteryStatusChanged>();
        await fixture.ShouldNotHavePublishedAsync<AgvBatteryChargeChanged>();
        fixture.VerifyBatteryMetricsCollected(Times.Once());
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_BatteryStatusChanged_PublishesAgvBatteryStatusChangedUpdatesBatteryChargeGauge()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs([agv]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, 50, 20, agv.AgvStatus!.Disturbed, agv.AgvStatus!.LastNodeId);

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvBatteryStatusChanged>();
        await fixture.ShouldNotHavePublishedAsync<AgvBatteryChargeChanged>();
        fixture.VerifyBatteryMetricsCollected(Times.Once());
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_EstimatedFullyChargedTime_PublishesAgvBatteryChargeChangedUpdatesBatteryChargeGauge()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        double newBatteryCharge = agv.AgvStatus!.BatteryCharge;
        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, newBatteryCharge, agv.AgvStatus!.Charging, agv.AgvStatus!.BatteryVoltage, agv.AgvStatus!.BatteryCurrent, agv.AgvStatus!.BatteryTemperature, agv.AgvStatus!.Disturbed, agv.AgvStatus!.LastNodeId, DateTime.UtcNow, agv.AgvStatus!.LastBatteryReplacement + TimeSpan.FromMinutes(2));

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvBatteryStatusChanged>();
        fixture.VerifyBatteryMetricsCollected(Times.Once());
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_LastNodeIdChanged_NewLastNodeIdIsOnGraph_PublishesAgvLastNodeIdChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, "newLastNodeName".ToGuid());

        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true)
            .WithNode(newAgvStatus.LastNodeId!.Value.ToGraphNode());
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
        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, Guid.NewGuid());

        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true)
            .WithNode([null]);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldNotHavePublishedAsync<AgvLastNodeIdChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChanged_PublishesAgvOnlineChanged()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([])
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
    public async Task Consume_UpdateAgvStatus_CurrentActionGroups_NoReservationNeeded_PublishesAgvOnlineChanged()
    {
        var lastNode = "somewhere".ToGraphNode();
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        ICollection<GraphNode> nodes = AgvGraphGenerator.GetNodesOfDefaultGraph(actionGroup.GetTargets());
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([actionGroup])
            .WithNodesStates([], nodes)
            .WithGraphExists(true)
            .AddAgvs([agv])
            .WithNode(lastNode);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, lastNodeId: lastNode.Id);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        fixture.VerifyAgvUpdated(Times.Once());
        fixture.VerifyNodeStateChange(forceReserve: Times.Never(), forceOccupy: Times.Never(), reserve: Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_LastNodeIdIsReservableNode_NodeStateWasDeleted_OccupiesLastNode()
    {
        var lastNode = AgvGraphGenerator.Prk1Node;
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        ICollection<GraphNode> nodes = AgvGraphGenerator.GetNodesOfDefaultGraph([lastNode.Id]);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([])
            .WithNodesStates([], nodes, lastNodeId: lastNode.Name)
            .WithGraphExists(true)
            .AddAgvs([agv]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, lastNodeId: lastNode.Id);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);

        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        fixture.VerifyAgvUpdated(Times.Once());
        fixture.VerifyNodeStateChange(forceReserve: Times.Never(), forceOccupy: Times.Once(), reserve: Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_CurrentActionGroups_ReservationNeeded_ReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId, targetNodeName: AgvGraphGenerator.Crg1);
        ICollection<GraphNode> nodes = AgvGraphGenerator.GetNodesOfDefaultGraph(actionGroup.GetTargets());
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([actionGroup])
            .WithNodesStates([], nodes)
            .WithGraphExists(true)
            .AddAgvs([agv]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        fixture.VerifyAgvUpdated(Times.Once());
        fixture.VerifyNodeStateChange(forceReserve: Times.Never(), forceOccupy: Times.Never(), reserve: Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_CurrentActionGroups_ReservationNeeded_NodeAlreadyReservedOrOccupied_PublishesCancelOrAbortActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId, targetNodeName: AgvGraphGenerator.Crg1);
        ICollection<GraphNode> nodes = AgvGraphGenerator.GetNodesOfDefaultGraph(actionGroup.GetTargets());
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([actionGroup])
            .WithNodesStates([], nodes, nodeReservationFails: true)
            .WithGraphExists(true)
            .AddAgvs([agv]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyAgvUpdated(Times.Once());
        fixture.VerifyNodeStateChange(forceReserve: Times.Never(), forceOccupy: Times.Never(), reserve: Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_CurrentActionGroups_ReservationNeeded_OldReservationNotDeleted_DoesNotRepeatReservation()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId, targetNodeName: AgvGraphGenerator.Crg1);
        ICollection<GraphNode> nodes = AgvGraphGenerator.GetNodesOfDefaultGraph(actionGroup.GetTargets());
        var nodeLock = NodeLockGenerator.GenerateNodeLock(nodeName: AgvGraphGenerator.Crg1, agvCorrelationId: agv.CorrelationId, nodeLockState: NodeLockState.Reserved);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([actionGroup])
            .WithNodesStates([nodeLock], nodes)
            .WithGraphExists(true)
            .AddAgvs([agv]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        await fixture.ShouldNotHavePublishedAsync<CancelOrAbortActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyAgvUpdated(Times.Once());
        fixture.VerifyNodeStateChange(forceReserve: Times.Never(), forceOccupy: Times.Never(), reserve: Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_CurrentActionGroups_ReservationNeeded_ActionGroupOutdated_DoesNotReserveNodePublishesCancelOrAbortActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId, targetNodeName: AgvGraphGenerator.Crg1, createdDate: DateTime.UtcNow.AddMinutes(-15));
        ICollection<GraphNode> nodes = AgvGraphGenerator.GetNodesOfDefaultGraph(actionGroup.GetTargets());
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([actionGroup])
            .WithNodesStates([], nodes)
            .WithGraphExists(true)
            .AddAgvs([agv]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
        fixture.VerifyAgvUpdated(Times.Once());
        fixture.VerifyNodeStateChange(forceReserve: Times.Never(), forceOccupy: Times.Never(), reserve: Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_CurrentActionGroups_QueuingActionGroup_ActionGroupOutdated_DoesNotReserveNodePublishesCancelOrAbortActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(assignedAgv: agv.CorrelationId, targetNodeId: AgvGraphGenerator.Way1, createdDate: DateTime.UtcNow.AddMinutes(-15));
        ICollection<GraphNode> nodes = AgvGraphGenerator.GetNodesOfDefaultGraph(actionGroup.GetTargets());
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([actionGroup])
            .WithNodesStates([], nodes)
            .WithGraphExists(true)
            .AddAgvs([agv]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
        fixture.VerifyAgvUpdated(Times.Once());
        fixture.VerifyNodeStateChange(forceReserve: Times.Never(), forceOccupy: Times.Never(), reserve: Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_CurrentActionGroups_QueuingActionGroup_ReservesNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(assignedAgv: agv.CorrelationId, targetNodeId: AgvGraphGenerator.Way1);
        ICollection<GraphNode> nodes = AgvGraphGenerator.GetNodesOfDefaultGraph(actionGroup.GetTargets());
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([actionGroup])
            .WithNodesStates([], nodes)
            .WithGraphExists(true)
            .AddAgvs([agv]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        await fixture.ShouldNotHavePublishedAsync<CancelOrAbortActionGroup>();
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ZoneIdsChanged_OnlineAgvIsInZone_StatusStored()
    {
        var agv = AgvGenerator.GenerateChargingAgv();
        agv.SetOnline();
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithZones([zone])
            .WithPostion(position)
            .WithGraphExists(true);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed, agv.AgvStatus.LastNodeId, agv.AgvStatus.EstimatedFullyChargedTime, agv.AgvStatus.LastBatteryReplacement, [zone.ZoneId]);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldNotHavePublishedAsync<UpdateErrorStatus>();
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [DataRow(null, new[] { "PS01" })]
    [DataRow(new string[] { }, new[] { "PS01" })]
    [DataRow(new[] { "PS01" }, new string[] { })]
    [DataRow(new[] { "PS01" }, null)]
    [DataRow(new[] { "PS01", "APL1" }, new[] { "APL1" })]
    [TestMethod]
    public async Task Consume_UpdateAgvStatus_StationIdsChanged_StatusStoredPublishesAgvStationsChanged(ICollection<string>? previous, ICollection<string>? current)
    {
        var agvCorrelationId = Guid.NewGuid();
        var status = Helper.AgvStatusGenerator.Generate(agvCorrelationId, stationIds: previous);
        var agv = AgvGenerator.GenerateRawAgv(agvCorrelationId, "1", status);
        agv.SetOnline();
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithZones([zone])
            .WithPostion(position)
            .WithGraphExists(true);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newStatus = Common.Testing.Helpers.AgvStatusGenerator.Generate(agvCorrelationId, stationIds: current);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newStatus);
        var stationsChanged = await fixture.PublishAndGetFirstMessageAsync<UpdateAgvStatus, AgvStationsChanged>(updateAgvStatus);
        stationsChanged.Stations.ShouldBeEquivalentTo(current?.ToList() ?? new List<string>());
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [DataRow(null, null)]
    [DataRow(new string[] { }, new string[] { })]
    [DataRow(new[] { "PS01" }, new[] { "PS01" })]
    [DataRow(new[] { "PS01", "APL1" }, new[] { "PS01", "APL1" })]
    [TestMethod]
    public async Task Consume_UpdateAgvStatus_StationIdsUnchanged_DoesNotPublishAgvStationsChanged(ICollection<string>? previous, ICollection<string>? current)
    {
        var agvCorrelationId = Guid.NewGuid();
        var status = Helper.AgvStatusGenerator.Generate(agvCorrelationId, stationIds: previous);
        var agv = AgvGenerator.GenerateRawAgv(agvCorrelationId, "1", status);
        agv.SetOnline();
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithZones([zone])
            .WithPostion(position)
            .WithGraphExists(true);
        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newStatus = Common.Testing.Helpers.AgvStatusGenerator.Generate(agvCorrelationId, stationIds: current);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldNotHavePublishedAsync<AgvStationsChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChangedForIdleAgv_PublishesAgvOnlineChangedAndBecameIdle()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true)
            .WithCurrentStartedOrCreatedActionGroups([])
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithActiveErrors([new FieldViolation(agv.CorrelationId, DateTime.UtcNow)]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, false, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        await fixture.ShouldNotHavePublishedAsync<UpdateErrorStatus>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_AliveStateChangedForIdleAgvWithZoneErrorAndResourceError_PublishesAgvOnlineChangedUpdateSystemAgvErrorsAndBecameIdle()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.GoOffline();
        agv.IsOnline.ShouldBeFalse();
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true)
            .WithCurrentStartedOrCreatedActionGroups([])
            .WithNodesStates([new NodeLock("node".ToGraphNode(), NodeLockState.Reserved, false, agv.CorrelationId, DateTime.UtcNow)], [])
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithActiveErrors([new ZoneError(agv.CorrelationId, DateTime.UtcNow, "zone"), new ResourceBlockingError(agv.CorrelationId, DateTime.UtcNow, "node".ToGuid())]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, false, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        var update = await fixture.GetFirstPublishedMessageAsync<UpdateSystemAgvErrors>();
        update.ErrorsToResolve.Single(e => e.IsResourceError()).ShouldBeOfType<ResourceBlockingError>();
        update.ErrorsToResolve.Single(e => e.IsZoneError()).ShouldBeOfType<ZoneError>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ZoneIdChanges_WithZoneErrorAndResourceError_PublishesUpdateSystemAgvErrors()
    {
        var zoneId = Guid.NewGuid();
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: [zoneId.ToString()]);
        var position = AgvPositionStatusGenerator.Generate(correlationId: agv.CorrelationId);
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true, zoneId);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithGraphExists(true)
            .WithZones([zone])
            .WithCurrentStartedOrCreatedActionGroups([])
            .WithNodesStates([new NodeLock("node".ToGraphNode(), NodeLockState.Reserved, false, agv.CorrelationId, DateTime.UtcNow)], [])
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithActiveErrors([new ZoneError(agv.CorrelationId, DateTime.UtcNow, "zone"), new ResourceBlockingError(agv.CorrelationId, DateTime.UtcNow, "node".ToGuid())]);

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, false, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        var update = await fixture.GetFirstPublishedMessageAsync<UpdateSystemAgvErrors>();
        update.ErrorsToResolve.Single(e => e.IsZoneError()).ShouldBeOfType<ZoneError>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvStatus_ShouldRequestData_PublishesRequestAgvData()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([])
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
            .WithCurrentStartedOrCreatedActionGroups([])
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
    public async Task Consume_UpdateAgvStatus_ShouldRequestZones_PublishesRequestAgvZones()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithCurrentStartedOrCreatedActionGroups([])
            .WithGraphExists(true);
        var agv = AgvGenerator.GenerateRawAgv();
        agv.ZonesExist = false;
        agv.AgvZonesRequestedLastTime = null;
        agv.AgvStatus = new TransportOrder.Domain.Models.Agv.AgvStatus();

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        var newAgvStatus = new AgvStatus(agv.CorrelationId, agv.AgvStatus!.OrderId, agv.AgvStatus!.Busy, agv.AgvStatus!.Automatic, agv.AgvStatus!.BatteryCharge, agv.AgvStatus!.Charging, 48, agv.AgvStatus!.Charging ? 20 : -10, 20, agv.AgvStatus!.Disturbed);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, newAgvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);
        await fixture.ShouldHavePublishedAsync<RequestAgvZones>();
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
    public async Task Consume_GoOffline_AgvWasOnline_NotInZone_PublishedAgvOnlineChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: []);
        agv.SetOnline();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithNodesStates([], [])
            .WithHasOnlineAgvs(false)
            .WithQueue(null);

        await fixture.PublishMessageAsync(new GoOffline(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        agv.IsOnline.ShouldBeFalse();
        await fixture.ShouldHavePublishedAsync<AllAgvsOffline>();
        await fixture.ShouldNotHavePublishedAsync<UpdateErrorStatus>();
        await fixture.ShouldNotHavePublishedAsync<AgvWentOfflineInZones>();
    }

    [TestMethod]
    public async Task Consume_GoOffline_AgvWasOnline_InZone_PublishedAgvOnlineChangedAndUpdateSystemAgvErrors()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneId]);
        agv.SetOnline();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithPostion(position)
            .WithZones([zone])
            .WithNodesStates([], [])
            .WithHasOnlineAgvs(false);

        await fixture.PublishMessageAsync(new GoOffline(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        agv.IsOnline.ShouldBeFalse();
        await fixture.ShouldHavePublishedAsync<AllAgvsOffline>();
        await fixture.ShouldHavePublishedAsync<UpdateSystemAgvErrors>();
        await fixture.ShouldHavePublishedAsync<AgvWentOfflineInZones>();
    }

    [TestMethod]
    public async Task Consume_GoOffline_AgvWasOnline_InQueue_PublishedAgvOnlineChangedAndUpdateSystemAgvErrors()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: []);
        agv.IsQueueing = true;
        agv.SetOnline();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithPostion(position)
            .WithZones([zone])
            .WithNodesStates([], [])
            .WithHasOnlineAgvs(false)
            .WithQueue(new TransportOrder.Domain.Models.Queue.Queue());

        await fixture.PublishMessageAsync(new GoOffline(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvOnlineChanged>();
        agv.IsOnline.ShouldBeFalse();
        await fixture.ShouldHavePublishedAsync<AllAgvsOffline>();
        await fixture.ShouldHavePublishedAsync<UpdateSystemAgvErrors>();
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
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: NodeGenerator.GenerateParkingNode().Name);
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
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        var agvInQueue = AgvGenerator.GenerateRawAgv(Guid.NewGuid(), "serial number");
        agvInQueue.IsQueueing = true;

        var queueHead = "a queue head".ToGraphNode();
        var agvsToMove = new Dictionary<Guid, Guid> { { agvInQueue.CorrelationId, queueHead.Id } };
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv, agvInQueue])
            .WithAgvsToMove(agvsToMove)
            .WithActionGroup(actionGroup)
            .WithEvacuationMoves([Guid.NewGuid()])
            .WithNode(queueHead);

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_WasQueuingAtQueueHeadNoOtherAgvsInQueue_AutomaticPickDropAction_QueueBecameEmpty()
    {
        var agvsToMove = new Dictionary<Guid, Guid>();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .WithAgvsToMove(agvsToMove)
            .WithActionGroup(actionGroup)
            .WithEvacuationMoves([Guid.NewGuid()]);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldHavePublishedAsync<QueueBecameEmpty>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_NotQueueing_ChargeAction_DoesNotPublishCreateQueueingActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "a node");
        agv.IsQueueing = false;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
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
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        agv.IsQueueing = true;
        var agvInQueue = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        agvInQueue.IsQueueing = true;
        var agvsToMove = new Dictionary<Guid, Guid> { { agvInQueue.CorrelationId, "queue node".ToGuid() } };

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        actionGroup.IsManual = true;
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv, agvInQueue])
            .WithAgvsToMove(agvsToMove)
            .WithActionGroup(actionGroup)
            .WithIsOnAQueueNode(true)
            .WithQueueInternalMoves([Guid.NewGuid()])
            .WithEvacuationMoves([Guid.NewGuid()]);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
        agv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_IsQueuing_ChargeAction_CreateQueueingActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        agv.IsQueueing = true;
        var agvInQueue = AgvGenerator.GenerateIdleAgv();
        agvInQueue.IsQueueing = true;
        var agvsToMove = new Dictionary<Guid, Guid> { { agvInQueue.CorrelationId, "queue node".ToGuid() } };

        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv, agvInQueue])
            .WithAgvsToMove(agvsToMove)
            .WithActionGroup(actionGroup)
            .WithIsOnAQueueNode(true)
            .WithQueueInternalMoves([Guid.NewGuid()])
            .WithEvacuationMoves([]);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
        agv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_IsQueuing_ChargeAction_NoOtherAgvsInQueue_QueueBecameEmpty()
    {
        var agvsToMove = new Dictionary<Guid, Guid>();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        agv.IsQueueing = true;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsToMove(agvsToMove, 0)
            .WithActionGroup(actionGroup)
            .WithIsOnAQueueNode(true)
            .WithQueueInternalMoves([])
            .WithEvacuationMoves([]);

        fixture.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv });

        await fixture.PublishMessageAsync(new PrepareAgvForStartingAction(actionGroup.Actions.First(), agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<AgvPreparedForStartingAction>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<QueueBecameEmpty>();
        await fixture.ShouldNotHavePublishedAsync<CancelOrAbortActionGroup>();
        agv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_PrepareAgvForStartingAction_AgvIsLocked_AgvPreparedForStartingActionNotPublished()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: NodeGenerator.GenerateParkingNode().Name, mode: new AgvMode(ModeType.Locked));
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
        var agvStatus = new AgvStatus(agv.CorrelationId, currentAgStatus.OrderId, currentAgStatus.Busy, currentAgStatus.Automatic, currentAgStatus.BatteryCharge, currentAgStatus.Charging, 50, 0, 20, currentAgStatus.Disturbed, currentAgStatus.LastNodeId, currentAgStatus.EstimatedFullyChargedTime, currentAgStatus.LastBatteryReplacement);
        var updateAgvStatus = new UpdateAgvStatus(agv.CorrelationId, agv.SerialNumber, agvStatus);
        await fixture.PublishMessageAsync(updateAgvStatus);

        await fixture.ShouldNotHavePublishedAsync<AgvBatteryStatusChanged>();
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
        await fixture.ShouldHavePublishedAsync<CheckAgvLoad>();
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
        await fixture.ShouldNotHavePublishedAsync<CheckAgvLoad>();
    }

    [TestMethod]
    public async Task Consume_PrepareIdleActionOfAgv_NoQueueChargerAvailable_PublishesChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .HasAlreadyReservedNode(false)
            .WithCurrentAssignedActionGroup(false)
            .WithEnqueueingTarget(null)
            .WithIsOnAQueueNode(false)
            .AddChargingNode(false, NodeGenerator.GenerateChargingNode("charge"))
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName))
            .WithInfiniteCharging(true);

        await fixture.PublishMessageAsync(new PrepareIdleActionOfAgv(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<QueueNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareIdleActionOfAgv_NoQueueIsOnCharger_PublishesChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .WithCurrentAssignedActionGroup(false)
            .WithEnqueueingTarget(null)
            .WithIsOnAQueueNode(false)
            .AddChargingNode(true, NodeGenerator.GenerateChargingNode("charge"))
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName))
            .WithInfiniteCharging(true);

        await fixture.PublishMessageAsync(new PrepareIdleActionOfAgv(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<QueueNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareIdleActionOfAgv_NoQueueFreeCharger_PublishesChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .WithCurrentAssignedActionGroup(false)
            .WithEnqueueingTarget(null)
            .WithIsOnAQueueNode(false)
            .AddChargingNode(false, NodeGenerator.GenerateChargingNode("charge"))
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName))
            .WithInfiniteCharging(true);

        await fixture.PublishMessageAsync(new PrepareIdleActionOfAgv(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<QueueNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareIdleActionOfAgv_NoQueueFreeCharger_NotInfinitelyCharging_PublishesParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .WithCurrentAssignedActionGroup(false)
            .WithEnqueueingTarget(null)
            .WithIsOnAQueueNode(false)
            .AddChargingNode(false, NodeGenerator.GenerateChargingNode("charge"))
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName))
            .WithInfiniteCharging(false);

        await fixture.PublishMessageAsync(new PrepareIdleActionOfAgv(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<QueueNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareIdleActionOfAgv_NoQueueNoCharger_PublishesParkingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .HasAlreadyReservedNode(false)
            .WithCurrentAssignedActionGroup(false)
            .WithEnqueueingTarget(null)
            .WithIsOnAQueueNode(false)
            .AddChargingNode(false, null)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName))
            .WithInfiniteCharging(true);

        await fixture.PublishMessageAsync(new PrepareIdleActionOfAgv(agv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<QueueNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public async Task Consume_PrepareIdleActionOfAgv_AgvIsLocked_DoesNotPublishIdleNodeSelectedForAgv(bool infiniteCharging)
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .AddActionGroupRetrieval(headingTo: false)
            .WithCurrentAssignedActionGroup(false)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName))
            .WithInfiniteCharging(infiniteCharging);

        await fixture.PublishMessageAsync(new PrepareIdleActionOfAgv(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<QueueNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public async Task Consume_PrepareIdleActionOfAgv_HasAssignedActionGroup_DoesNotPublishParkingNodeSelectedForAgv(bool infiniteCharging)
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string parkNodeName = "Park-1";

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsGraph()
            .HasAlreadyReservedNode(true)
            .WithCurrentAssignedActionGroup(true)
            .AddParkingPointManager(parkGraphNode: NodeGenerator.GenerateParkingNode(parkNodeName))
            .WithInfiniteCharging(infiniteCharging);

        await fixture.PublishMessageAsync(new PrepareIdleActionOfAgv(agv.CorrelationId));

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
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_AgvIsOnCharger_PublishesChargingNodeSelectedForAgv()
    {
        const string chargeNodeName = "Charger-1";
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: chargeNodeName);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .WithAgvIsOnCharger(true)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([])
            .AddChargingPointManager(NodeGenerator.GenerateChargingNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
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
        await fixture.PublishMessageAsync(command);

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
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHaveConsumedAsync<PrepareChargingOfAgv>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_HasChargingActionGroupAlready_DoesNotPublishChargingNodeSelectedForAgv()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        const string chargeNodeName = "Charger-1";
        var chargingAction = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([chargingAction])
            .AddChargingPointManager(NodeGenerator.GenerateChargingNode(chargeNodeName));

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.PublishMessageAsync(command);

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
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
    }

    [TestMethod]
    public async Task Consume_PrepareChargingOfAgv_NoChargerAvailable_AgvMustCharge_PublishCriticalAgvHasNoCharger()
    {
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 1);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .HasAlreadyReservedNode(false)
            .WithUnfinishedActionGroup(false)
            .WithActionGroups([])
            .AddChargingPointManager(null);

        var command = new PrepareChargingOfAgv(agv.CorrelationId);
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<CriticalAgvHasNoCharger>();
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
        await fixture.PublishMessageAsync(command);

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
        await fixture.PublishMessageAsync(command);

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
        await fixture.PublishMessageAsync(command);

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
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AgvModeChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.InitiatingUser.ShouldBe(initiatingUser);
        await fixture.ShouldHavePublishedAsync<CheckAgvLoad>();
    }

    [TestMethod]
    public async Task Consume_SetNormalMode_ValidData_PublishesAgvModeChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Maintenance));
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);

        var command = new SetNormalMode(agv.CorrelationId, null);
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AgvModeChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.Mode.InitiatingUser.ShouldBeNull();

        await fixture.ShouldHavePublishedAsync<CheckAgvLoad>();
    }

    [TestMethod]
    public async Task Consume_SetNormalMode_AgvIsLocked_PublishesAgvModeChanged()
    {
        var agv = AgvGenerator.GenerateIdleAgv(mode: new AgvMode(ModeType.Locked));
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);

        var command = new SetNormalMode(agv.CorrelationId, null);
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<AgvModeChanged>();
        await fixture.ShouldHavePublishedAsync<AgvBecameIdle>();
        agv.Mode.Type.ShouldBe(ModeType.Normal);
        agv.IsLocked.ShouldBeFalse();
        agv.Mode.InitiatingUser.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<CheckAgvLoad>();
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
    [TestMethod]
    public async Task Consume_SwitchMode_ValidData_PublishesAgvModeChanged(ModeType previousType, ModeType pendingType)
    {
        var mode = new AgvMode(previousType, Guid.NewGuid());
        mode.PrepareSwitch(pendingType);
        var agv = AgvGenerator.GenerateIdleAgv(mode: mode);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);

        await fixture.PublishMessageAsync(new SwitchMode(agv.CorrelationId));

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

        await fixture.PublishMessageAsync(new TryEnqueueAgv(Guid.NewGuid(), "a queue node".ToGuid()));

        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<QueueEntered>();
    }

    [TestMethod]
    public async Task Consume_TryEnqueueAgv_AgvEnqueuesAtQueueEntry_DoesNotPublishCreateQueueingActionGroup()
    {
        const string entryNode = "entry node";
        var fixture = new AgvCommandHandlerTestFixture()
            .WithEnqueueingTarget(entryNode);

        await fixture.PublishMessageAsync(new TryEnqueueAgv(Guid.NewGuid(), entryNode.ToGuid()));

        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<QueueEntered>();
    }

    [TestMethod]
    public async Task Consume_TryEnqueueAgv_ValidData_PublishesCreateQueueingActionGroup()
    {
        var fixture = new AgvCommandHandlerTestFixture()
            .WithEnqueueingTarget("target queue node");

        await fixture.PublishMessageAsync(new TryEnqueueAgv(Guid.NewGuid(), "queue entry node".ToGuid()));

        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<QueueEntered>();
    }

    [TestMethod]
    public async Task Consume_ReceiveAgvZones_ValidData_PublishesAddZone()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);

        var message = new ReceiveAgvZones(agv.CorrelationId, agv.SerialNumber, [new AgvZone("zone1", 1, [new PolygonPoint(1, 1)], []), new AgvZone("zone2", 2, [new PolygonPoint(2, 2)], [])]);

        await fixture.PublishMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AddZone>();

        fixture.TestHarness.Published.Select<AddZone>().Count().ShouldBe(message.AgvZones.Count);
        fixture.VerifyAgvUpdated(Times.Once());
        agv.ZonesExist.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZones_ValidData_PublishesUpdateSystemAgvErrors()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneId]);
        agv.AgvStatus!.ZoneIds!.Count.ShouldBe(1);
        agv.GoOffline();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithPostion(position)
            .WithZones([zone])
            .WithActiveErrors([new ZoneError(agv.CorrelationId, DateTime.UtcNow, zone.ZoneId)]);

        await fixture.PublishMessageAsync(new RemoveAgvFromAllZones(agv.CorrelationId, "user"));

        await fixture.ShouldHavePublishedAsync<UpdateSystemAgvErrors>();
        agv.AgvStatus!.ZoneIds.ShouldBeEmpty();
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZone_ValidData_PublishesUpdateSystemAgvErrors()
    {
        var position = AgvPositionStatusGenerator.Generate();
        var zone = ZoneHelper.GenerateZoneFromPosition(position, isInside: true);
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone.ZoneId]);
        agv.AgvStatus!.ZoneIds!.Count.ShouldBe(1);
        agv.GoOffline();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .WithPostion(position)
            .WithZones([zone])
            .WithActiveErrors([new ZoneError(agv.CorrelationId, DateTime.UtcNow, zone.ZoneId)]);

        await fixture.PublishMessageAsync(new RemoveAgvFromZone(agv.CorrelationId, zone.ZoneId, "user"));
        await fixture.ShouldHavePublishedAsync<UpdateSystemAgvErrors>();
        agv.AgvStatus!.ZoneIds.ShouldBeEmpty();
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvs_AgvIsInQueue_PublishesChargingNodeSelectedForAgv_CreateQueueingActionGroup()
    {
        var queueingCriticalBatteryAgv = AgvGenerator.GenerateIdleAgv(1);
        var agvToMove = AgvGenerator.GenerateIdleAgv(2);
        agvToMove.IsQueueing = true;
        var chargingAgv = AgvGenerator.GenerateChargingAgv(batteryCharge: 80);
        const string queueEntryNode = "node";
        var queueNode = "queue node".ToGraphNode();
        var agvsToMove = new Dictionary<Guid, Guid> { { agvToMove.CorrelationId, queueNode.Id } };

        const string charger = "charger";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agvToMove, queueingCriticalBatteryAgv, chargingAgv])
            .WithNodesStates([new NodeLock(charger.ToGraphNode(), NodeLockState.Occupied, false, chargingAgv.CorrelationId, DateTime.UtcNow)], [])
            .WithAgvsToMove(agvsToMove, nodeName: queueEntryNode, swapped: true)
            .WithQueueInternalMoves([])
            .WithNode(queueNode);

        await fixture.PublishMessageAsync(new SwitchChargingAgvs(queueingCriticalBatteryAgv.CorrelationId, chargingAgv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvsFailed>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();

        List<CreateQueueingActionGroup> createQueueActionGroupMessages = fixture.TestHarness.Published.Select<CreateQueueingActionGroup>().Select(m => m.Context.Message).ToList();
        createQueueActionGroupMessages.Count.ShouldBe(2);
        createQueueActionGroupMessages.Single(m => m.AgvCorrelationId == chargingAgv.CorrelationId).QueueNode.Id.ShouldBe(queueEntryNode.ToGuid());
        createQueueActionGroupMessages.Single(m => m.AgvCorrelationId == agvToMove.CorrelationId).QueueNode.Id.ShouldBe(queueNode.Id);

        var chargingNodeSelectedForAgv = fixture.TestHarness.Published.Select<ChargingNodeSelectedForAgv>().Select(m => m.Context.Message).Single();
        chargingNodeSelectedForAgv.ChargeStationId.ShouldBe(charger.ToGuid());
        chargingNodeSelectedForAgv.CorrelationId.ShouldBe(queueingCriticalBatteryAgv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvs_AgvIsInNotQueue_QueueAvailable_PublishesChargingNodeSelectedForAgv_CreateQueueingActionGroup()
    {
        var criticalBatteryAgv = AgvGenerator.GenerateIdleAgv(1);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(batteryCharge: 80);
        const string queueEntryNode = "node";

        const string charger = "charger";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([criticalBatteryAgv, chargingAgv])
            .WithNodesStates([new NodeLock(charger.ToGraphNode(), NodeLockState.Occupied, false, chargingAgv.CorrelationId, DateTime.UtcNow)], [])
            .WithAgvsToMove(null, nodeName: queueEntryNode, swapped: true)
            .WithQueueInternalMoves([]);

        await fixture.PublishMessageAsync(new SwitchChargingAgvs(criticalBatteryAgv.CorrelationId, chargingAgv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvsFailed>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();

        List<CreateQueueingActionGroup> createQueueActionGroupMessages = fixture.TestHarness.Published.Select<CreateQueueingActionGroup>().Select(m => m.Context.Message).ToList();
        createQueueActionGroupMessages.Count.ShouldBe(1);
        createQueueActionGroupMessages.Single(m => m.AgvCorrelationId == chargingAgv.CorrelationId).QueueNode.Id.ShouldBe(queueEntryNode.ToGuid());

        var chargingNodeSelectedForAgv = fixture.TestHarness.Published.Select<ChargingNodeSelectedForAgv>().Select(m => m.Context.Message).Single();
        chargingNodeSelectedForAgv.ChargeStationId.ShouldBe(charger.ToGuid());
        chargingNodeSelectedForAgv.CorrelationId.ShouldBe(criticalBatteryAgv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvs_AgvIsInNotQueue_NoQueueButParkerAvailable_PublishesChargingNodeSelectedForAgv_ParkingNodeSelectedForAgv()
    {
        var criticalBatteryAgv = AgvGenerator.GenerateIdleAgv(1);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(batteryCharge: 80);
        const string queueEntryNode = "node";

        const string charger = "charger";
        const string parker = "parker";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([criticalBatteryAgv, chargingAgv])
            .WithNodesStates([new NodeLock(charger.ToGraphNode(), NodeLockState.Occupied, false, chargingAgv.CorrelationId, DateTime.UtcNow)], [])
            .WithAgvsToMove(null, nodeName: queueEntryNode, swapped: false)
            .AddParkingPointManager(parker.ToGraphNode());

        await fixture.PublishMessageAsync(new SwitchChargingAgvs(criticalBatteryAgv.CorrelationId, chargingAgv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldHavePublishedAsync<ParkingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvsFailed>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();

        var parkingNodeSelectedForAgv = fixture.TestHarness.Published.Select<ParkingNodeSelectedForAgv>().Select(m => m.Context.Message).Single();
        parkingNodeSelectedForAgv.ParkingNode.Name.ShouldBe(parker);
        parkingNodeSelectedForAgv.CorrelationId.ShouldBe(chargingAgv.CorrelationId);

        var chargingNodeSelectedForAgv = fixture.TestHarness.Published.Select<ChargingNodeSelectedForAgv>().Select(m => m.Context.Message).Single();
        chargingNodeSelectedForAgv.ChargeStationId.ShouldBe(charger.ToGuid());
        chargingNodeSelectedForAgv.CorrelationId.ShouldBe(criticalBatteryAgv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvs_AgvIsInNotQueue_NoQueueNoParkerAvailable_PublishesChargingNodeSelectedForAgv_QueueActionGroup()
    {
        const string lastNodeId = "a node";
        var criticalBatteryAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeName: lastNodeId);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(batteryCharge: 80);

        var chargerNode = "charger".ToGraphNode();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([criticalBatteryAgv, chargingAgv])
            .WithNodesStates([new NodeLock(chargerNode, NodeLockState.Occupied, false, chargingAgv.CorrelationId, DateTime.UtcNow)], [])
            .WithAgvsToMove(null, nodeName: null, swapped: false)
            .AddParkingPointManager(null);

        await fixture.PublishMessageAsync(new SwitchChargingAgvs(criticalBatteryAgv.CorrelationId, chargingAgv.CorrelationId));

        await fixture.ShouldHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<SwitchChargingAgvsFailed>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();

        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().Select(m => m.Context.Message).Single();
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(chargingAgv.CorrelationId);
        queueActionGroup.ActionGroup.Actions.Single().Target.Node!.Id.ShouldBe(lastNodeId.ToGuid());

        var chargingNodeSelectedForAgv = fixture.TestHarness.Published.Select<ChargingNodeSelectedForAgv>().Select(m => m.Context.Message).Single();
        chargingNodeSelectedForAgv.ChargeStationId.ShouldBe(chargerNode.Id);
        chargingNodeSelectedForAgv.CorrelationId.ShouldBe(criticalBatteryAgv.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvs_AgvIsInNotQueue_NoQueueNoParkerAvailableNoLastNodeId_PublishesSwitchChargingAgvsFailed()
    {
        const string? lastNodeId = null;
        var criticalBatteryAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeName: lastNodeId);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(batteryCharge: 80);

        const string charger = "charger";
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([criticalBatteryAgv, chargingAgv])
            .WithNodesStates([new NodeLock(charger.ToGraphNode(), NodeLockState.Occupied, false, chargingAgv.CorrelationId, DateTime.UtcNow)], [])
            .WithAgvsToMove(null, nodeName: null, swapped: false)
            .AddParkingPointManager(null);

        await fixture.PublishMessageAsync(new SwitchChargingAgvs(criticalBatteryAgv.CorrelationId, chargingAgv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<SwitchChargingAgvsFailed>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_SwitchChargingAgvs_AgvNotOnCharger_PublishesSwitchChargingAgvsFailed()
    {
        const string? lastNodeId = null;
        var criticalBatteryAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeName: lastNodeId);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(batteryCharge: 80);

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([criticalBatteryAgv, chargingAgv])
            .WithNodesStates([], [])
            .WithAgvsToMove(null, nodeName: null, swapped: false)
            .AddParkingPointManager(null);

        await fixture.PublishMessageAsync(new SwitchChargingAgvs(criticalBatteryAgv.CorrelationId, chargingAgv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<ChargingNodeSelectedForAgv>();
        await fixture.ShouldNotHavePublishedAsync<AbortChargeAction>();
        await fixture.ShouldNotHavePublishedAsync<CreateQueueingActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<ParkingNodeSelectedForAgv>();
        await fixture.ShouldHavePublishedAsync<SwitchChargingAgvsFailed>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CheckAgvLoad_NoCurrentActionGroups_DoesNotPublishUpdateErrorStatus()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithActionGroups([])
            .WithHasCargoStates(true);

        await fixture.PublishMessageAsync(new CheckAgvLoad(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<UpdateErrorStatus>();
    }

    [TestMethod]
    public async Task Consume_CheckAgvLoad_MatchingState_NoPreviousCargoMismatchErrors_DoesNotPublishUpdateErrorStatus()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Created, assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithActionGroups([actionGroup])
            .WithHasCargoStates(true);

        await fixture.PublishMessageAsync(new CheckAgvLoad(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<UpdateErrorStatus>();
    }

    [TestMethod]
    public async Task Consume_CheckAgvLoad_MatchingState_PreviousCargoMismatchErrors_PublishesUpdateSystemAgvErrors()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Created, assignedAgv: agv.CorrelationId);

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithActionGroups([actionGroup])
            .WithHasCargoStates(true)
            .WithActiveErrors([new CargoMismatchError(agv.CorrelationId, DateTime.UtcNow, false)]);

        var updateErrorStatus = await fixture.PublishAndGetFirstMessageAsync<CheckAgvLoad, UpdateSystemAgvErrors>(new CheckAgvLoad(agv.CorrelationId));

        updateErrorStatus.ErrorsToAdd.ShouldBeEmpty();
        updateErrorStatus.ErrorsToResolve.Single().ShouldBeOfType<CargoMismatchError>();
    }

    [TestMethod]
    public async Task Consume_CheckAgvLoad_Mismatch_PreviousCargoMismatchErrors_DoesNotPublishUpdateErrorStatus()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Created, assignedAgv: agv.CorrelationId);

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithActionGroups([actionGroup])
            .WithHasCargoStates(false)
            .WithActiveErrors([new CargoMismatchError(agv.CorrelationId, DateTime.UtcNow, false)]);

        await fixture.PublishMessageAsync(new CheckAgvLoad(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<UpdateErrorStatus>();
    }

    [TestMethod]
    public async Task Consume_CheckAgvLoad_Mismatch_NoPreviousCargoMismatchErrors_PublishesUpdateSystemAgvErrors()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Created, assignedAgv: agv.CorrelationId);

        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithActionGroups([actionGroup])
            .WithHasCargoStates(false)
            .WithActiveErrors([]);

        var updateErrorStatus = await fixture.PublishAndGetFirstMessageAsync<CheckAgvLoad, UpdateSystemAgvErrors>(new CheckAgvLoad(agv.CorrelationId));

        updateErrorStatus.ErrorsToAdd.Single().ShouldBeOfType<CargoMismatchError>();
        updateErrorStatus.ErrorsToResolve.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_CheckAgvLoad_NoStartedActionGroup_UsesCompletedActionGroup()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created, dropActionState: ActionState.Created, assignedAgv: agv.CorrelationId);
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionStateMove: ActionState.Completed, actionStateCharge: ActionState.Completed, assignedAgv: agv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithActionGroups([actionGroup, chargeActionGroup])
            .WithHasCargoStates(true)
            .WithActiveErrors([]);

        var update = await fixture.PublishAndGetFirstMessageAsync<CheckAgvLoad, UpdateSystemAgvErrors>(new CheckAgvLoad(agv.CorrelationId));

        update.ErrorsToAdd.Single().ShouldBeOfType<CargoMismatchError>();
    }

    [TestMethod]
    public async Task Consume_CheckAgvLoad_NoStartedActionGroup_PausedActionGroupStartedAfterCompletedActionGroup_UsesPausedActionGroup()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Created, dropActionState: ActionState.Created, assignedAgv: agv.CorrelationId);
        var completedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(pickActionState: ActionState.Completed, dropActionState: ActionState.Completed, assignedAgv: agv.CorrelationId, startedDate: DateTime.UtcNow.AddMinutes(-10));

        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(actionStateMove: ActionState.Paused, actionStateCharge: ActionState.Created, assignedAgv: agv.CorrelationId, startedDate: DateTime.UtcNow);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithActionGroups([actionGroup, completedActionGroup, chargeActionGroup])
            .WithHasCargoStates(true)
            .WithActiveErrors([]);

        var update = await fixture.PublishAndGetFirstMessageAsync<CheckAgvLoad, UpdateSystemAgvErrors>(new CheckAgvLoad(agv.CorrelationId));

        update.ErrorsToAdd.Single().ShouldBeOfType<CargoMismatchError>();
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromQueue_EscapeQueue_PublishesUpdateErrorStatus()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        agv.IsQueueing = true;
        var agvInQueue = AgvGenerator.GenerateRawAgv(Guid.NewGuid(), "serial number");
        agvInQueue.IsQueueing = true;

        var queueHead = "a queue head".ToGraphNode();
        var agvsToMove = new Dictionary<Guid, Guid> { { agvInQueue.CorrelationId, queueHead.Id } };
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv, agvInQueue])
            .WithAgvsToMove(agvsToMove)
            .WithAgvRemovedFromQueueEntry(false)
            .WithActiveErrors([new QueueBlockingError(agv.CorrelationId, DateTime.UtcNow)])
            .WithQueueInternalMoves([])
            .WithEvacuationMoves([])
            .WithNode(queueHead);

        var update = await fixture.PublishAndGetFirstMessageAsync<RemoveAgvFromQueue, UpdateSystemAgvErrors>(new RemoveAgvFromQueue(agv.CorrelationId));

        update.ErrorsToAdd.ShouldBeEmpty();
        update.ErrorsToResolve.Single().ShouldBeOfType<QueueBlockingError>();
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromQueue_RemovedFromQueueEntryNode_PublishesUpdateSystemAgvErrors()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        agv.IsQueueing = true;
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsToMove(new Dictionary<Guid, Guid>())
            .WithAgvRemovedFromQueueEntry(true)
            .WithActiveErrors([new QueueBlockingError(agv.CorrelationId, DateTime.UtcNow)])
            .WithQueueInternalMoves([])
            .WithEvacuationMoves([]);

        var update = await fixture.PublishAndGetFirstMessageAsync<RemoveAgvFromQueue, UpdateSystemAgvErrors>(new RemoveAgvFromQueue(agv.CorrelationId));

        update.ErrorsToAdd.ShouldBeEmpty();
        update.ErrorsToResolve.Single().ShouldBeOfType<QueueBlockingError>();
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromQueue_NotInQueueAnymore_DoesNotPublishUpdateSystemAgvErrors()
    {
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "queue node");
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv])
            .WithAgvsToMove(new Dictionary<Guid, Guid>())
            .WithAgvRemovedFromQueueEntry(false)
            .WithActiveErrors([])
            .WithQueueInternalMoves([])
            .WithEvacuationMoves([]);

        await fixture.PublishMessageAsync(new RemoveAgvFromQueue(agv.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<UpdateSystemAgvErrors>();
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZones_ValidData_AgvUpdated()
    {
        const string zone = "zone";
        var agv = AgvGenerator.GenerateIdleAgv(zoneIds: [zone]);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgvs([agv]);

        await fixture.PublishMessageAsync(new RemoveAgvFromZones(agv.CorrelationId, [zone]));

        agv.AgvStatus?.ZoneIds.ShouldBeEmpty();
        fixture.VerifyAgvUpdated(Times.Once());
    }
}