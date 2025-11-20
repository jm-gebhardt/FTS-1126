using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Moq;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using AgvStatusGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvStatusGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class MetricsCollectionEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionAbortedByAgv_NoAssignedAgv_DoesNotCollectStationOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new ActionAbortedByAgv(actionGroup.Actions.First().CorrelationId, Guid.NewGuid()));
        fixture.VerifyStationOrderCompletionMetricsCollected(Times.Never());
    }

    [TestMethod]
    public async Task Consume_ActionAbortedByAgv_ReassignedToDifferentAgv_DoesNotCollectStationOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new ActionAbortedByAgv(actionGroup.Actions.First().CorrelationId, Guid.NewGuid()));
        fixture.VerifyStationOrderCompletionMetricsCollected(Times.Never());
    }

    [DataRow(ActionState.Aborted)]
    [DataRow(ActionState.Completed)]
    [DataRow(ActionState.Canceled)]
    [TestMethod]
    public async Task Consume_ActionAbortedByAgv_ActionInWrongState_DoesNotCollectStationOrderCompletionMetrics(ActionState state)
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: state);
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new ActionAbortedByAgv(actionGroup.Actions.First().CorrelationId, Guid.NewGuid()));
        fixture.VerifyStationOrderCompletionMetricsCollected(Times.Never());
    }

    [TestMethod]
    public async Task Consume_ActionAbortedByAgv_ChargeActionAborted_CollectsStationOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: Guid.NewGuid(), actionStateCharge: ActionState.Aborted);
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new ActionAbortedByAgv(actionGroup.Actions.Last().CorrelationId, actionGroup.AssignedAgv!.Value));
        fixture.VerifyStationOrderCompletionMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_ValidData_CollectsStationOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: ActionState.Completed);
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.First().CorrelationId, DateTime.UtcNow));
        fixture.VerifyStationOrderCompletionMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_ValidData_CollectsStationOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: ActionState.Completed, dropActionState: ActionState.Completed);
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Last().CorrelationId, DateTime.UtcNow));
        fixture.VerifyStationOrderCompletionMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_ChargeActionCompleted_ValidData_CollectsStationOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: Guid.NewGuid(), actionStateCharge: ActionState.Aborted);
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new ChargeActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Last().CorrelationId));
        fixture.VerifyStationOrderCompletionMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_ValidData_CollectsOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new ActionGroupCompleted(AgvActionGenerator.ConvertToMessage(actionGroup), actionGroup.CorrelationId, DateTime.UtcNow));
        fixture.VerifyOrderCompletionMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_ActionAborted_ValidData_CollectsOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), pickActionState: ActionState.Aborted);
        var abortedAction = actionGroup.Actions.Single(a => a.State == ActionState.Aborted);
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        await fixture.PublishMessageAsync(new ActionAborted(abortedAction.CorrelationId, AgvActionGenerator.ConvertToMessage(actionGroup).Actions.Single(a => a.State == ActionState.Aborted)));
        fixture.VerifyOrderCompletionMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_SaveActionGroupAbortion_ValidData_CollectsOrderCompletionMetrics()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: Guid.NewGuid(), actionStateMove: ActionState.Started);
        var fixture = new MetricsCollectionEventHandlerTestFixture()
            .AddActionGroup(actionGroup);
        var command = new SaveActionGroupAbortion(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).CorrelationId, AbortReason.ReactivatedFromCharging);

        await fixture.PublishMessageAsync(command);

        fixture.VerifyOrderCompletionMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvStatusReceived_ValidData_CollectsBatteryChargeMetrics()
    {
        var status = AgvStatusGenerator.Generate();
        var fixture = new MetricsCollectionEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new AgvStatusReceived(status.CorrelationId, "serial number", fixture.Mapper.Map<AgvStatus>(status)));

        fixture.VerifyBatteryMetricsCollected();
    }

    [TestMethod]
    public async Task Consume_AgvErrorChanged_ValidData_CollectsSafetyFieldViolationMetrics()
    {
        var fieldViolation = new FieldViolation(Guid.NewGuid(), DateTime.UtcNow);
        var fixture = new MetricsCollectionEventHandlerTestFixture();
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, fieldViolation.AgvCorrelationId, fieldViolation.ErrorCode!, fieldViolation.ErrorLevel, fieldViolation.ErrorCategory);

        await fixture.PublishMessageAsync(new AgvErrorChanged(fieldViolation.CorrelationId, errorStatus.Errors, errorStatus.Errors, []));

        fixture.VerifySafetyFieldViolationMetricsCollected();
    }

    [TestMethod]
    public async Task Consume_JamStateChanged_ValidData_CollectsJamStateMetrics()
    {
        var fixture = new MetricsCollectionEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new JamStateChanged(Guid.NewGuid(), true, DateTime.UtcNow.AddMinutes(-1), false, DateTime.UtcNow));

        fixture.VerifyJamStateMetricsCollected();
    }
}