using Gebhardt.Acc.Common.Messages.Triggers;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TriggerService.Domain.Models;

/// <summary>
/// A trigger entity.
/// </summary>
[PublicAPI]
public class Trigger
{
    /// <inheritdoc cref="Trigger"/>>
    public Trigger(Guid id, ICollection<ConfigurationParameter> configurationParameters)
    {
        Id = id;
        ConfigurationParameters = configurationParameters;
    }

    /// <inheritdoc cref="Trigger"/>>
    private Trigger()
    {
        Id = Guid.Empty;
        ConfigurationParameters = [];
    }

    /// <summary>
    ///  The id of the trigger.
    /// </summary>
   public Guid Id { get; set; }

    /// <summary>
    /// The configuration parameters of the trigger.
    /// </summary>
    public ICollection<ConfigurationParameter> ConfigurationParameters { get; private set; }

    /// <summary>
    /// Update parameters
    /// </summary>
    /// <param name="updatedParameters"></param>
    public void UpdateParameters(ICollection<ConfigurationParameter> updatedParameters)
    {
        foreach (var updatedParameter in updatedParameters)
        {
            var parameter = ConfigurationParameters.SingleOrDefault(p => p.Name == updatedParameter.Name);
            if (parameter != null)
            {
                parameter.Value = updatedParameter.Value;
            }
        }
    }
}