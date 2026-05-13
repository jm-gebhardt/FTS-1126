using System.Text.Json;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

/// <summary>
/// Provides extension methods form <see cref="MqttActionParameter"/>
/// </summary>
public static class MqttActionParametersExtension
{
    extension(ICollection<MqttActionParameter> parameters)
    {
        /// <summary>
        /// Extract the value of a string parameter
        /// </summary>
        /// <param name="key">The parameter key</param>
        public string? GetStringParameter(string key)
        {
            object? value = parameters.SingleOrDefault(a => string.Equals(a.Key, key))?.Value;
            return value is JsonElement je ? je.GetString() : value as string;
        }

        /// <summary>
        /// Extract the value of a float parameter
        /// </summary>
        /// <param name="key">The parameter key</param>
        public float? GetFloatParameter(string key)
        {
            object? value = parameters.SingleOrDefault(a => string.Equals(a.Key, key))?.Value;
            if (value is JsonElement je && je.TryGetSingle(out float f))
            {
                return f;
            }
            if (value is float floatVal)
            {
                return floatVal;
            }
            return null;
        }
    }
}