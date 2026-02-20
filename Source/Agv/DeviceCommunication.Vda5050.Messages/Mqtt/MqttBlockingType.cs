using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

/// <summary>
/// Blocking behaviour of an action during order execution.
/// </summary>
[PublicAPI]
public enum MqttBlockingType
{
    /// <summary>Action can happen in parallel with others, including movement.</summary>
    None,

    /// <summary>Action can happen simultaneously with others, but not while moving.</summary>
    Soft,

    /// <summary>No other actions can be performed while this action is running.</summary>
    Hard
}
