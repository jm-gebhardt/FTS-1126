using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// An action defined for a vehicle type on an edge.
/// </summary>
[PublicAPI]
public class EdgeActionParameter
{
    /// <inheritdoc cref="EdgeActionParameter"/>
    public EdgeActionParameter(string key, string value)
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
