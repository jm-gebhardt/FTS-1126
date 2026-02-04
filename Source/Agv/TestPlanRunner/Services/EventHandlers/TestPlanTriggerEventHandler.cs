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

    /// <inheritdoc cref="TestPlanTriggerEventHandler"/>
    public TestPlanTriggerEventHandler(
        ITestPlanManager testPlanManager,
        ILogger<TestPlanTriggerEventHandler> logger
    )
    {
        _logger = logger;
        _testPlanManager = testPlanManager;
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
        var agvCorrelationId = context.Message.CorrelationId;

        _logger.LogDebug("AGV {AgvCorrelationId} completed action group (last action type: {ActionType}, Id: {ActionGroupId}), checking for test plan actions",
            agvCorrelationId,
            context.Message.ActionGroup.Actions.Last().ActionType,
            context.Message.ActionGroup.CorrelationId);

        try
        {
            await _testPlanManager.TryStartAgvAsync(agvCorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling AGV {AgvCorrelationId} completed action group event", agvCorrelationId);
        }
    }
}