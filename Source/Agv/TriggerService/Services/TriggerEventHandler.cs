using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TriggerService.Domain.Services;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TriggerService.Services;

/// <summary>
/// Handles trigger related events.
/// </summary>
[UsedImplicitly]
public class TriggerEventHandler(ITriggerManager triggerManager, ILogger<TriggerEventHandler> logger) : 
    IConsumer<AgvReachedTrigger>,
    IConsumer<AgvStartedActionGroupOnTrigger>,
    IConsumer<DefaultTriggerConfigurationUpdated>,
    IConsumer<AgvLeftTrigger>
{
    /// <summary>
    /// Handles the <see cref="AgvReachedTrigger"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvReachedTrigger> context)
    {
        var (triggerId, parameters, agvData, agvStatus, actionGroup, cargo, errors) = context.Message;

        logger.LogInformation("Invoking enter trigger {TriggerId} for agv {AgvId}", triggerId, agvStatus.CorrelationId);

        await triggerManager.InvokeTrigger(triggerId, parameters, agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <summary>
    /// Handles the <see cref="AgvStartedActionGroupOnTrigger"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStartedActionGroupOnTrigger> context)
    {
        var (triggerId, parameters, agvData, agvStatus, actionGroup, cargo, errors) = context.Message;

        logger.LogInformation("Invoking action group change trigger {TriggerId} for agv {AgvId}", triggerId, agvStatus.CorrelationId);

        await triggerManager.InvokeActionGroupChangedOnTrigger(triggerId, parameters, agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <summary>
    /// Handles the <see cref="DefaultTriggerConfigurationUpdated"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<DefaultTriggerConfigurationUpdated> context)
    {
        var (triggerId, updatedParameters) = context.Message;
        await triggerManager.UpdateDefaultConfigurationParameters(triggerId, updatedParameters);
    }

    /// <summary>
    /// Handles the <see cref="AgvLeftTrigger"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvLeftTrigger> context)
    {
        var (triggerId, parameters, agvData, agvStatus, actionGroup, cargo, errors) = context.Message;

        logger.LogInformation("Invoking leave trigger {TriggerId} for agv {AgvId}", triggerId, agvStatus.CorrelationId);

        await triggerManager.LeaveTrigger(triggerId, parameters, agvData, agvStatus, actionGroup, cargo, errors);
    }
}