using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TriggerService.Configuration;
using Gebhardt.Acc.TriggerService.Domain.Repositories;

namespace Gebhardt.Acc.TriggerService.Domain.Services;

/// <summary>
/// Manages triggers
/// </summary>
[UsedImplicitly]
public class TriggerManager(ReflectionService reflectionService, ILogger<TriggerManager> logger, TriggerServiceConfiguration configuration, ITriggerRepository triggerRepository): ITriggerManager
{
    /// <inheritdoc cref="ITriggerManager"/>>
    public async Task<IReadOnlyCollection<ITrigger>> LoadTriggers()
    {
        var triggers = new List<ITrigger>();

        foreach (var trigger in reflectionService.GetITriggerImplementations(configuration.TriggerFolder))
        {
            var triggerInstance = reflectionService.GetInstance<ITrigger>(trigger, []);
            if (triggerInstance == null)
            {
                logger.LogError("Could not create instance of trigger {Trigger}", trigger.FullName);
                break;
            }
            var triggerProperties = triggerInstance.GetProperties();
            if ( await triggerRepository.TryAdd(triggerProperties.Id, triggerInstance))
            {
                triggers.Add(triggerInstance);
                logger.LogInformation("Trigger configured: {Id} / {Name} / {Description}", triggerProperties.Id, triggerProperties.DisplayName, triggerProperties.Description);
            }
        }
        return triggers;
    }

    /// <inheritdoc cref="ITriggerManager"/>>
    public async Task InvokeTrigger(Guid triggerId, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        var trigger = await triggerRepository.GetById(triggerId);
        var triggerInfo = trigger.GetProperties();
        logger.LogInformation("Invoke trigger: {Id} / {Name} for agv {AgvCorrelationId}", triggerInfo.Id, triggerInfo.DisplayName, agvStatus.CorrelationId);
        await trigger.Invoke(agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <inheritdoc cref="ITriggerManager"/>>
    public async Task UpdateConfigurationParameters(Guid triggerId, ICollection<ConfigurationParameter> updatedParameters)
    {
        var trigger = await triggerRepository.GetById(triggerId);
        foreach (var parameter in updatedParameters)
        {
            trigger.UpdateConfigurationParameter(parameter.Name, parameter.Value);
        }

        await triggerRepository.UpdateTrigger(triggerId, trigger);
    }
}