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
    public async Task<IReadOnlyCollection<IFfcTrigger>> LoadTriggers()
    {
        var triggers = new List<IFfcTrigger>();

        foreach (var trigger in reflectionService.GetITriggerImplementations(configuration.TriggerFolder))
        {
            var triggerInstance = reflectionService.GetInstance<IFfcTrigger>(trigger, []);
            if (triggerInstance == null)
            {
                logger.LogError("Could not create instance of trigger {Trigger}", trigger.FullName);
                break;
            }
            var triggerProperties = triggerInstance.GetProperties();
            await triggerRepository.AddOrUpdate(triggerProperties.Id, triggerInstance);
            triggers.Add(triggerInstance);
            logger.LogInformation("Trigger configured: {Id} / {Name} / {Description}", triggerProperties.Id, triggerProperties.DisplayName, triggerProperties.Description);
        }
        return triggers;
    }

    /// <inheritdoc cref="ITriggerManager"/>>
    public async Task InvokeTrigger(Guid triggerId, Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        var trigger = await triggerRepository.GetById(triggerId);
        var triggerInfo = trigger.GetProperties();
        logger.LogInformation("Invoke trigger: {Id} / {Name} for agv {AgvCorrelationId}", triggerInfo.Id, triggerInfo.DisplayName, agvStatus.CorrelationId);
        await trigger.Invoke(parameters, agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <inheritdoc cref="ITriggerManager"/>>
    public async Task LeaveTrigger(Guid triggerId, Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        var trigger = await triggerRepository.GetById(triggerId);
        var triggerInfo = trigger.GetProperties();
        logger.LogInformation("Leave trigger: {Id} / {Name} for agv {AgvCorrelationId}", triggerInfo.Id, triggerInfo.DisplayName, agvStatus.CorrelationId);
        await trigger.Leave(parameters, agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <inheritdoc cref="ITriggerManager"/>
    public async Task InvokeActionGroupChangedOnTrigger(Guid triggerId, Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        var trigger = await triggerRepository.GetById(triggerId);
        var triggerInfo = trigger.GetProperties();
        logger.LogInformation("Invoke action group change for trigger: {Id} / {Name} for agv {AgvCorrelationId}", triggerInfo.Id, triggerInfo.DisplayName, agvStatus.CorrelationId);
        await trigger.InvokeActionGroupChanged(parameters, agvData, agvStatus, actionGroup, cargo, errors);
    }

    /// <inheritdoc cref="ITriggerManager"/>>
    public async Task UpdateDefaultConfigurationParameters(Guid triggerId, ICollection<ConfigurationParameter> updatedParameters)
    {
        var trigger = await triggerRepository.GetById(triggerId);
        foreach (var parameter in updatedParameters)
        {
            logger.LogInformation("Updating trigger configuration parameter {Name}: {Value}", parameter.Name, parameter.Value);
            trigger.UpdateDefaultConfigurationParameter(parameter.Name, parameter.Value);
        }

        await triggerRepository.UpdateTrigger(triggerId, trigger);
    }
}