
using JetBrains.Annotations;

namespace Gebhardt.Acc.StationCommunication.Domain.Models;

/// <summary>
/// Base information of a station
/// </summary>
[PublicAPI]
public class Station
{
    /// <summary>
    /// The name of the station
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// True, if the station uses jam states.
    /// </summary>
    public bool HasJamState { get; set; }

    /// <inheritdoc cref="Station"/>
    public Station(string name, bool hasJamState)
    {
        Name = name;
        HasJamState = hasJamState;
    }

    /// <inheritdoc cref="Station"/>
    public Station()
    {
    }
}