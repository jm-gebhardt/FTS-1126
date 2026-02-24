using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

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
        Guid? agvCorrelationId = context.Message.ActionGroup.AssignedAgv;
        if (agvCorrelationId == null)
        {
            _logger.LogDebug("ActionGroupCompleted has no assigned AGV, skipping");
            return;
        }

        var actionGroupCorrelationId = context.Message.CorrelationId;

        _logger.LogDebug("AGV {AgvCorrelationId} completed action group (last action type: {ActionType}, Id: {ActionGroupId}), checking for test plan actions",
            agvCorrelationId,
            context.Message.ActionGroup.Actions.Last().ActionType,
            actionGroupCorrelationId);

        try
        {
            var state = _executionStateProvider.GetOrCreate(agvCorrelationId.Value);
            if (state.CurrentHostOrderCorrelationId == actionGroupCorrelationId)
            {
                _logger.LogDebug("AGV {AgvCorrelationId} completed HostOrder {HostOrderCorrelationId}",
                    agvCorrelationId, actionGroupCorrelationId);
                state.CurrentHostOrderCorrelationId = null;
            }

            await _testPlanManager.TryStartAgvAsync(agvCorrelationId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling AGV {AgvCorrelationId} completed action group event", agvCorrelationId);
        }
    }
}