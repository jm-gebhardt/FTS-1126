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
public class TestTrigger(IBus bus) : IFfcTrigger
{
    private const string PropertyName = "Number";
    private const string PropertyDescription = "Test1 Trigger Description";
    private int _propertyValue;

    /// <inheritdoc cref="IFfcTrigger"/>>
    public ConcurrentDictionary<string, ConfigurationParameter> GetDefaultConfigurationParameters()
    {
        var config = new ConcurrentDictionary<string, ConfigurationParameter>();
        config.TryAdd(PropertyName, new ConfigurationParameter(PropertyName, PropertyDescription, _propertyValue.ToString(), @"^-?\d+$"));
        return config;
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public void UpdateDefaultConfigurationParameter(string name, string value)
    {
        _propertyValue = int.Parse(value);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public TriggerProperties GetProperties()
    {
        const string name = "Test1_Trigger";
        return new TriggerProperties("www.gebhardt-group.com:TEST1", name, "Test 1 trigger.", instancesMax: 3);
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public async Task Invoke(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        var parameter = parameters.GetValueOrDefault(PropertyName) ??
                                    GetDefaultConfigurationParameters().GetValueOrDefault(PropertyName);
        _propertyValue = int.Parse(parameter!.Value);
       
        Console.WriteLine($"{PropertyName}: {_propertyValue}");
        Console.WriteLine(agvStatus.CorrelationId.ToString());
        await bus.Publish(new Success());
    }

    /// <inheritdoc cref="IFfcTrigger"/>>
    public Task InvokeActionGroupChanged(Dictionary<string, ConfigurationParameter> parameters, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        return Task.CompletedTask;
    }
}