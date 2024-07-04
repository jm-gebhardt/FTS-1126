using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TriggerExample;

/// <summary>
/// Represents a trigger example
/// </summary>
[UsedImplicitly]
public class TestTrigger(IBus bus) : ITrigger
{
    private const string PropertyName = "Set my value";
    private int _propertyValue;

    /// <inheritdoc cref="ITrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        config.TryAdd(PropertyName, new ConfigurationParameter(PropertyName, _propertyValue.ToString(), ""));
        return config;
    }

    /// <inheritdoc cref="ITrigger"/>>
    public void UpdateConfigurationParameter(string name, string value)
    {
        _propertyValue = int.Parse(value);
    }

    /// <inheritdoc cref="ITrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "test trigger";
        return new TriggerProperties("www.wobe-systems.com:", name, "");
    }

    /// <inheritdoc cref="ITrigger"/>>
    public async Task Invoke(AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        Console.WriteLine($"{PropertyName}: {_propertyValue}");
        Console.WriteLine(agvStatus.CorrelationId.ToString());
        await bus.Publish(new Success());
    }
}