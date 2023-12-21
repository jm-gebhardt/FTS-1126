using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper.AgvActionGenerator;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

[TestClass]
public class AgvEventHandlerTest
{
    private readonly IMapper _mapper = AgvEventHandlerTestFixture.GetMapper();

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvIsLocked_PublishesTryStartManualAction()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        agv.Lock();
        fixture.AddAgv(agv);
        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvNeedsCharging_AgvNotReadyForBusy_NothingPublished()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();

        fixture.AddAgv(agv);

        var agvNeedsCharging = new AgvNeedsCharging(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(agvNeedsCharging);

        (await fixture.ConsumerHarness.Consumed.Any<AgvNeedsCharging>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNextActionInActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<StartChargingActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartNewActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsLocked_PublishesTryStartManualAction()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        agv.Lock();

        fixture.AddAgv(agv);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvCarryingLoad_PublishesTryStartNextActionInActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");
        fixture.AddAgv(agv);
        fixture.AgvHasUnfinishedDropAction(agv.CorrelationId);
        fixture.AddChargeAction(existsPriorToTest: false);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNextActionInActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingLoadDoesNotNeedsChargingOrNeedMaintenance_PublishesTryStartNewActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId");

        fixture.AddAgv(agv);
        fixture.AddChargeAction(existsPriorToTest: false);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNewActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvIsNotCarryingNeedCharging_DoesNotPublishTryStartNewActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "nodeId", batteryCharge: 10);

        fixture.AddAgv(agv);
        fixture.AddChargeAction(existsPriorToTest: false);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNextActionInActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartNewActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_AgvNotReadyForBusy_NothingPublished()
    {
        var fixture = new AgvEventHandlerTestFixture();

        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgv(agv);

        var agvBecameIdle = new AgvBecameIdle(agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(agvBecameIdle);

        (await fixture.ConsumerHarness.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryStartNextActionInActionGroup>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartManualAction>()).ShouldBeFalse();
        (await fixture.TestHarness.Published.Any<TryStartNewActionGroup>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ChargingAgvCanHandleActionsAgain_ValidData_PublishesTryReactivateChargingAgv()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();

        await fixture.TestHarness.Bus.Publish(new ChargingAgvCanHandleActionsAgain(agv.CorrelationId));
        (await fixture.ConsumerHarness.Consumed.Any<ChargingAgvCanHandleActionsAgain>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvCanStartIdleAction_AgvWithBatteryLevelGood_PublishesCreateParkingActionGroup()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new AgvCanStartIdleAction(agv.CorrelationId));
        (await fixture.ConsumerHarness.Consumed.Any<AgvCanStartIdleAction>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CreateParkingActionGroup>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ChargingAgvCanHandleActionsAgain_ValidData_TryReactivateChargingAgv()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new ChargingAgvCanHandleActionsAgain(agv.CorrelationId));
        (await fixture.ConsumerHarness.Consumed.Any<ChargingAgvCanHandleActionsAgain>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelCritical, AgvEventHandlerTestFixture.BatteryLevelCritical)]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelLow, AgvEventHandlerTestFixture.BatteryLevelCritical)]
    public async Task Consume_AgvBatteryChargeChanged_publish_AgvNeedsCharging(int oldCharge, int newCharge)
    {
        const string agvId = "AGV001";
        var correlationId = agvId.ToGuid();

        var fixture = new AgvEventHandlerTestFixture();
        await fixture.TestHarness.Bus.Publish(new AgvBatteryChargeChanged(correlationId, oldCharge, newCharge));

        bool consumeCheck = await fixture.ConsumerHarness.Consumed.Any<AgvBatteryChargeChanged>(s => s.Context.Message.CorrelationId == correlationId);
        consumeCheck.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelCritical, AgvEventHandlerTestFixture.BatteryLevelGood)]
    [DataRow(AgvEventHandlerTestFixture.BatteryLevelLow, AgvEventHandlerTestFixture.BatteryLevelFull)]
    public async Task Consume_AgvBatteryChargeChanged_publish_ChargingAgvsCanHandleActionsAgainEvent(int oldCharge, int newCharge)
    {
        const string agvId = "AGV001";
        var correlationId = agvId.ToGuid();

        var fixture = new AgvEventHandlerTestFixture();
        await fixture.TestHarness.Bus.Publish(new AgvBatteryChargeChanged(correlationId, oldCharge, newCharge));

        bool consumeCheck = await fixture.ConsumerHarness.Consumed.Any<AgvBatteryChargeChanged>(s => s.Context.Message.CorrelationId == correlationId);
        consumeCheck.ShouldBeTrue();

        bool publishCheck = await fixture.TestHarness.Published.Any<ChargingAgvCanHandleActionsAgain>();
        publishCheck.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvMapReceived_ValidData_PublishesSaveMap()
    {
        const string agvId = "AGV001";
        var agvCorrelationId = agvId.ToGuid();
        var map = new Map(new Guid(), "", DateTime.MaxValue, "", 0, 0, 0.5f, 0.5f, 0.5f);

        var fixture = new AgvEventHandlerTestFixture()
            .WithAgvGraph(AgvGraphGenerator.CreateDefaultGraph());
        await fixture.TestHarness.Bus.Publish(new AgvMapReceived(agvCorrelationId, agvId, map));

        (await fixture.ConsumerHarness.Consumed.Any<AgvMapReceived>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<SaveMap>(f => f.Context.Message.Map.CorrelationId == map.CorrelationId)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvStatusReceived_AgvInRepository_PublishesUpdateAgvStatus()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(agv.CorrelationId, agv.AgvId, _mapper.Map<AgvStatus>(agv.AgvStatus!)));
        (await fixture.ConsumerHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<UpdateAgvStatus>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ScheduledAgvWentOffline_ValidData_PublishesGoOffline()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        ScheduledMessage<AgvWentOffline> offlineMessage = await fixture.ScheduleMessage(TimeSpan.FromMilliseconds(0), new AgvWentOffline(agv.CorrelationId, DateTime.UtcNow));
        agv.SetOnline(offlineMessage.TokenId);
        fixture.AddAgv(agv);
        (await fixture.ConsumerHarness.Consumed.Any<AgvWentOffline>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<GoOffline>()).ShouldBeTrue();
        var goOfflineCommand = fixture.TestHarness.Published.Select<GoOffline>().First().Context.Message;
        goOfflineCommand.OfflineTokenId.ShouldBe(offlineMessage.TokenId);
    }

    [TestMethod]
    public async Task Consume_AgvPreparedForStartingAction_ValidData_StartAction()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);
        await fixture.TestHarness.Bus.Publish(new AgvPreparedForStartingAction(agv.CorrelationId, actionGroup.Actions.First()));

        (await fixture.ConsumerHarness.Consumed.Any<AgvPreparedForStartingAction>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<StartAction>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvLoadStatusReceived_UpdateAgvLoadStatus()
    {
        var fixture = new AgvEventHandlerTestFixture();
        var agvCorrelationId = Guid.NewGuid();
        ICollection<Cargo> cargos = CargoGenerator.GenerateCargos(agvCorrelationId);
        var agvLoadStatusReceived = new AgvLoadStatusReceived(agvCorrelationId, cargos.ToList().AsReadOnly());
        await fixture.TestHarness.Bus.Publish(agvLoadStatusReceived);
        (await fixture.ConsumerHarness.Consumed.Any<AgvLoadStatusReceived>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<UpdateCargo>()).ShouldBeTrue();
        var updateLoadStatus = fixture.TestHarness.Published.Select<UpdateCargo>().First().Context.Message;
        updateLoadStatus.CorrelationId.ShouldBe(agvCorrelationId);
        updateLoadStatus.Cargos.ShouldBeEquivalentTo(cargos);
    }
}