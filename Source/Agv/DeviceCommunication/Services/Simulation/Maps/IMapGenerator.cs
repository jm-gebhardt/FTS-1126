using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Maps;

/// <summary>
/// Generates a Map including a PNG image.
/// </summary>
public interface IMapGenerator
{
    /// <summary>
    /// Checks if the generator can be used. This is e.g. not the case if the file is not existing on the disk.
    /// </summary>
    /// <param name="mapName">The name of the map.</param>
    /// <returns>Yes if it can provide a map, otherwise no.</returns>
    bool CanBeUsed(string mapName);

    /// <summary>
    /// Generates the Map.
    /// </summary>
    /// <param name="mapId">The unique Id of the map.</param>
    /// <param name="mapName">The name of the map.</param>
    /// <param name="graph">The graph which is included in the map.</param>
    /// <returns>Map in PNG format plus additional attributes concerning scaling.</returns>
    Map Generate(Guid mapId, string mapName, IGraph graph);
}