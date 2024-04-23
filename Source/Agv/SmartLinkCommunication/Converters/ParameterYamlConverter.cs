using CommandLine;
using Gebhardt.Acc.SmartLinkCommunication.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Gebhardt.Acc.SmartLinkCommunication.Converters;

/// <summary>
/// Custom converter for Parameter property of IoActionConfig which has an object type to be able to pass any parameters to it.
/// </summary>
public class ParameterYamlConverter : IYamlTypeConverter
{
    /// <inheritdoc />
    public bool Accepts(Type type)
    {
        return type == typeof(List<object>);
    }

    /// <inheritdoc />
    public object ReadYaml(IParser parser, Type type)
    {
        var typesToTry = new List<Type> { typeof(ActionDefinition) }; // Add other types as needed

        foreach (var targetType in typesToTry)
        {
            try
            {
                var listType = typeof(List<>).MakeGenericType(targetType);

                var deserializer = new DeserializerBuilder().Build();
                object? obj = deserializer.Deserialize(parser, listType);
                obj = ((IEnumerable<object>?)obj?.Cast<object>())?.ToList();

                if (obj != null)
                {
                    return obj;
                }
            }
            catch (YamlException)
            {
                // Ignore and move to next type
            }
        }

        throw new YamlException("Unable to deserialize the provided YAML into any of the expected types.");
    }

    /// <inheritdoc />
    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        // Implement custom writing logic here if needed, 
        // or let the default serializer handle it.
        var serializer = new SerializerBuilder().Build();
        serializer.Serialize(emitter, value);
    }
}