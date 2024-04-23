using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;

namespace Gebhardt.Acc.DeviceCommunication.Exceptions;

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