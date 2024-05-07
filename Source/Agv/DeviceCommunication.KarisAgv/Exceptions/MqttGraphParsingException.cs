using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Exceptions;

/// <summary>
/// Represents the exception thrown when parsing the <see cref="MqttGraph"/> failed.
/// </summary>
public class MqttGraphParsingException : Exception
{
    /// <inheritdoc cref="MqttGraphParsingException"/>>
    public MqttGraphParsingException(string error) : base($"Parsing graph failed: {error}")
    {
    }
}