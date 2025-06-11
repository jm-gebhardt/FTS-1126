using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.TriggerDefinition;

using ConfigurationParameters = ConcurrentDictionary<string, ConfigurationParameter>;

/// <summary>
/// Provides an interface for fusion flow control trigger implementations.
/// </summary>
[PublicAPI]
public interface IFfcTrigger
{
    /// <summary>
    /// Gets the default configuration parameters of the trigger.
    /// </summary>
    /// <returns></returns>
    ConfigurationParameters GetDefaultConfigurationParameters();

    /// <summary>
    /// Updates a default configuration parameter of the trigger.
    /// </summary>
    /// <param name="name">The parameter name. </param>
    /// <param name="value">The parameter value.</param>
    void UpdateDefaultConfigurationParameter(string name, string value);

    /// <summary>
    /// Get trigger properties.
    /// </summary>
    TriggerProperties GetProperties();

    /// <summary>
    /// Invoke trigger.
    /// </summary>
    Task Invoke(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors);

    /// <summary>
    /// Invoke trigger on action group change.
    /// </summary>
    Task InvokeActionGroupChanged(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors);
}