using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// A station of the graph.
/// </summary>
/// <param name="Id">The id of the station.</param>
/// <param name="Name">The name of the station.</param>
/// <param name="ShortName">The short name of the station.</param>
/// <param name="X">The x position in [m] in reference to the global origin.</param>
/// <param name="Y">The y position in [m] in reference to the global origin.</param>
/// <param name="Theta">The absolute orientation of the station in [rad].</param>
/// <param name="StationHeight">The absolute physical height of the station in [m].</param>
/// <param name="InteractionNodeIds">The ids of the nodes at which the interaction with the station takes place.</param>
/// <param name="Capabilities">The actions AGVs can run at the station.</param>
/// <param name="Status">The current state of the station.</param>
[PublicAPI]
public record StationDto(
    Guid Id,
    string Name,
    string ShortName,
    double X,
    double Y,
    double? Theta,
    double? StationHeight,
    IReadOnlyList<Guid> InteractionNodeIds,
    IReadOnlyList<RoutingActionType> Capabilities,
    StationStatusDto Status);