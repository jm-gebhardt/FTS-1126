using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.TriggerDefinition;

using ConfigurationParameters = ConcurrentDictionary<string, ConfigurationParameter>;

/// <summary>
/// Provides an interface for fusion flow control trigger implementations
/// </summary>
[PublicAPI]
public interface IFfcTrigger
{
    /// <summary>
    /// Gets the configuration parameters of the trigger.
    /// </summary>
    /// <returns></returns>
    ConfigurationParameters GetConfigurationParameters();

    /// <summary>
    /// Updates a configuration parameter of the trigger
    /// </summary>
    /// <param name="name">The parameter name. </param>
    /// <param name="value">The parameter value.</param>
    void UpdateConfigurationParameter(string name, string value);

    /// <summary>
    /// Get trigger properties
    /// </summary>
    /// <returns></returns>
    TriggerProperties GetProperties();

    /// <summary>
    /// Invoke trigger.
    /// </summary>
    Task Invoke(AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors);
}