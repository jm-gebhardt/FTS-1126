using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

namespace Gebhardt.Acc.OrderTranslator.Domain.Models.Station;

/// <summary>
/// A station.
/// </summary>
[PublicAPI]
public class Station
{
    /// <summary>
    /// The id of the station.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The name of the station
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// True, if the station requires to report cargo states.
    /// </summary>
    public bool NeedsCargoReporting { get; set; }

    /// <summary>
    /// True, if the station requires to report pallets.
    /// </summary>
    public bool NeedsPalletReporting { get; set; }

    /// <summary>
    /// Routing, how to approach the station for a pick.
    /// </summary>
    public ICollection<Routing> PickRouting
    {
        get;
        set => field = value ?? [];
    } = [];

    /// <summary>
    /// Routing, how to approach the station for a pick.
    /// </summary>
    public ICollection<Routing> DropRouting
    {
        get;
        set => field = value ?? [];
    } = [];

    /// <summary>
    /// Other stations in the same group.
    /// </summary>
    public ICollection<string> StationGroup
    {
        get;
        set => field = value ?? [];
    } = [];

    /// <summary>
    /// The Doppelspiel mode supported for the station when picking.
    /// </summary>
    public DoppelspielMode PickDoppelspielMode { get; set; }

    /// <summary>
    /// The Doppelspiel mode supported for the station when picking.
    /// </summary>
    public DoppelspielMode DropDoppelspielMode { get; set; }

    /// <summary>
    /// The conveyor input lanes leading to the station.
    /// </summary>
    public ICollection<string> InputLanes
    {
        get;
        set => field = value ?? [];
    } = [];

    /// <summary>
    /// The conveyor output lanes leading from the station.
    /// </summary>
    public ICollection<string> OutputLanes
    {
        get;
        set => field = value ?? [];
    } = [];

    /// <inheritdoc cref="Station"/>
    public Station(string name, bool needsCargoReporting, bool needsPalletReporting, DoppelspielMode pickDoppelspielMode, DoppelspielMode dropDoppelspielMode, ICollection<string> inputLanes, ICollection<string> outputLanes, ICollection<Routing> pickRouting, ICollection<Routing> dropRouting, ICollection<string> stationGroup)
    {
        Id = name.ToGuid();
        Name = name;
        NeedsCargoReporting = needsCargoReporting;
        NeedsPalletReporting = needsPalletReporting;
        PickRouting = pickRouting;
        DropRouting = dropRouting;
        PickDoppelspielMode = pickDoppelspielMode;
        DropDoppelspielMode = dropDoppelspielMode;
        InputLanes = inputLanes;
        OutputLanes = outputLanes;
        StationGroup = stationGroup;
    }

    /// <inheritdoc cref="Station"/>
    public Station()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// A routing description.
/// </summary>
[PublicAPI]
public record Routing
{
    /// <summary>The source or destination station.</summary>
    public string Station { get; init; } = string.Empty;

    /// <summary>The target for an intermediate move.</summary>
    public string MoveTarget { get; init; } = string.Empty;

    /// <inheritdoc cref="Routing"/>
    public Routing()
    {
    }

    /// <inheritdoc cref="Routing"/>
    public Routing(string station, string moveTarget)
    {
        Station = station;
        MoveTarget = moveTarget;
    }
}