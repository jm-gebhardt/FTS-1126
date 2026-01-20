using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class TriggerGenerator
{
    public static ConfigurationParameter CreateParameter(
        string name = "parameter name",
        string description = "description",
        string value = "0",
        string unit = "")
    {
        return new ConfigurationParameter(name, description, value, unit);
    }

    public static Trigger CreateTrigger(
        Guid? id = null,
        string urn = "urn",
        string name = "a trigger",
        string version = "version",
        string description = "description",
        int? instanceMaximum = null,
        ICollection<ConfigurationParameter>? parameters = null,
        bool isDeleted = false)
    {
        List<ConfigurationParameter> finalParameters = parameters == null ? [CreateParameter()] : [..parameters];

        return new Trigger(
            id ?? Guid.NewGuid(),
            urn,
            name,
            version,
            description,
            instanceMaximum,
            finalParameters
        )
        {
            IsDeleted = isDeleted
        };
    }

    public static List<Trigger> CreateTriggers(int count = 1)
    {
        var list = new List<Trigger>();
        for (int i = 0; i < count; i++)
        {
            list.Add(CreateTrigger());
        }
        return list;
    }
}