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
    IConsumer<DefaultTriggerConfigurationUpdated>
{
    /// <summary>
    /// Handles the <see cref="AgvReachedTrigger"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvReachedTrigger> context)
    {
        (var triggerId, Dictionary<string, ConfigurationParameter> parameters, var agvData, var agvStatus, var actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors) = context.Message;

        await triggerManager.InvokeTrigger(triggerId, parameters, agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <summary>
    /// Handles the <see cref="DefaultTriggerConfigurationUpdated"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<DefaultTriggerConfigurationUpdated> context)
    {
        (var triggerId, ICollection<ConfigurationParameter> updatedParameters) = context.Message;
        await triggerManager.UpdateDefaultConfigurationParameters(triggerId, updatedParameters);
    }
}