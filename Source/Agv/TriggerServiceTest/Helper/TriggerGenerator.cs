using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.TriggerDefinition;

namespace Gebhardt.Acc.TriggerService.Test.Helper;

public static class TriggerGenerator
{
    public class Trigger : IFfcTrigger
    {
        public Trigger()
        {
            Parameters.TryAdd("name", new ConfigurationParameter("name", "value", "a validation regex"));
        }

        public ConcurrentDictionary<string, ConfigurationParameter> Parameters { get; } = new();

        public TriggerProperties Properties { get; } = new("unique", "name", "description");

        public ConcurrentDictionary<string, ConfigurationParameter> GetConfigurationParameters()
        {
            return Parameters;
        }

        public void UpdateConfigurationParameter(string name, string value)
        {
            Parameters.TryGetValue(name, out var oldParameter);
            Parameters.TryUpdate(name, new ConfigurationParameter(oldParameter!.Name, value, oldParameter.Validation), oldParameter);
        }

        public TriggerProperties GetProperties()
        {
            return Properties;
        }

        public Task Invoke(AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
        {
            return Task.CompletedTask;
        }
    }

    public static Trigger GenerateTrigger()
    {
        return new Trigger();
    }
}