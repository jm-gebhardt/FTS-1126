using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// An action defined for a vehicle type on a node.
/// </summary>
[PublicAPI]
public class NodeActionParameter
{
    /// <inheritdoc cref="NodeActionParameter"/>
    public NodeActionParameter(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Key of the action parameter.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Value of the action parameter.
    /// </summary>
    public string Value { get; set; }
}
