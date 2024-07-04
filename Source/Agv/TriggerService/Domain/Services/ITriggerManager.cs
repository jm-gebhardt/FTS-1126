using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;

namespace Gebhardt.Acc.TriggerService.Domain.Services;

/// <summary>
/// Provides an interface for trigger manager implementations.
/// </summary>
public interface ITriggerManager
{
    /// <summary>
    /// Load the triggers
    /// </summary>
    Task<IReadOnlyCollection<ITrigger>> LoadTriggers();

    /// <summary>
    /// Invoke the trigger
    /// </summary>
    /// <param name="triggerId">The id of the trigger.</param>
    /// <param name="agvData">The agv data.</param>
    /// <param name="agvStatus">The agv status.</param>
    /// <param name="actionGroup">The current action group.</param>
    /// <param name="cargo">The current cargo.</param>
    /// <param name="errors">Agv errors.</param>
    Task InvokeTrigger(Guid triggerId, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors);

    /// <summary>
    /// Update configuration parameters of a trigger.
    /// </summary>
    /// <param name="triggerId">The id of the trigger.</param>
    /// <param name="updatedParameters">The updated configuration parameters.</param>
    Task UpdateConfigurationParameters(Guid triggerId, ICollection<ConfigurationParameter> updatedParameters);
}