using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TestPlanRunner.Services.EventHandlers;

/// <summary>
/// Handles AgvBecameIdle events to trigger test plan execution.
/// </summary>
[UsedImplicitly]
public class TestPlanTriggerEventHandler : IConsumer<AgvBecameIdle>, IConsumer<ActionGroupCompleted>
{
    private readonly ILogger<TestPlanTriggerEventHandler> _logger;
    private readonly ITestPlanManager _testPlanManager;
    private readonly IAgvExecutionStateProvider _executionStateProvider;

    /// <inheritdoc cref="TestPlanTriggerEventHandler"/>
    public TestPlanTriggerEventHandler(
        ITestPlanManager testPlanManager,
        IAgvExecutionStateProvider executionStateProvider,
        ILogger<TestPlanTriggerEventHandler> logger)
    {
        _logger = logger;
        _testPlanManager = testPlanManager;
        _executionStateProvider = executionStateProvider;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvBecameIdle> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        _logger.LogDebug("AGV {AgvCorrelationId} became idle, checking for test plan actions", agvCorrelationId);

        try
        {
            await _testPlanManager.TryStartAgvAsync(agvCorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling AGV {AgvCorrelationId} became idle event", agvCorrelationId);
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var actionGroupCorrelationId = context.Message.CorrelationId;
        var agvCorrelationId = context.Message.ActionGroup.AssignedAgv;
        if (agvCorrelationId == null)
        {
            _logger.LogDebug("ActionGroupCompleted {ActionGroupId} has no assigned AGV, skipping", actionGroupCorrelationId);
            return;
        }

        _logger.LogDebug("AGV {AgvCorrelationId} completed action group (last action type: {ActionType}, Id: {ActionGroupId}), checking for test plan actions",
            agvCorrelationId,
            context.Message.ActionGroup.Actions.Last().ActionType,
            actionGroupCorrelationId);

        try
        {
            var state = _executionStateProvider.GetOrCreate(agvCorrelationId.Value);
            if (state.CurrentStepCorrelationId != actionGroupCorrelationId)
            {
                _logger.LogDebug("AGV {AgvCorrelationId} completed action group {ActionGroupId} but waiting for {ExpectedId}, skipping",
                    agvCorrelationId, actionGroupCorrelationId, state.CurrentStepCorrelationId);
                return;
            }
            _logger.LogDebug("AGV {AgvCorrelationId} marking action group {ActionGroupId} as completed",
                agvCorrelationId, actionGroupCorrelationId);
            
            await _testPlanManager.SetStepCompletedAsync(agvCorrelationId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling AGV {AgvCorrelationId} completed action group event", agvCorrelationId);
        }
    }
}