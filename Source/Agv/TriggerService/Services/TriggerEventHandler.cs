using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TriggerService.Domain.Services;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TriggerService.Services;

/// <summary>
/// Handles trigger related events.
/// </summary>
/// <param name="triggerManager"></param>
[UsedImplicitly]
public class TriggerEventHandler(ITriggerManager triggerManager): 
    IConsumer<AgvReachedTrigger>,
    IConsumer<TriggerConfigurationUpdated>
{
    /// <summary>
    /// Handles the <see cref="AgvReachedTrigger"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvReachedTrigger> context)
    {
        (var triggerId, var agvData, var agvStatus, var actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors) = context.Message;

        await triggerManager.InvokeTrigger(triggerId, agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <summary>
    /// Handles the <see cref="TriggerConfigurationUpdated"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerConfigurationUpdated> context)
    {
        (var triggerId, ICollection<ConfigurationParameter> updatedParameters) = context.Message;
        await triggerManager.UpdateConfigurationParameters(triggerId, updatedParameters);
    }
}