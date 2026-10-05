using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ExportedStation"/>
/// </summary>
public static class ExportedStationExtension
{
    /// <summary>
    /// Converts a station to a graph node.
    /// </summary>
    /// <param name="station"></param>
    /// <returns></returns>
    public static GraphNode ToGraphNode(this ExportedStation station)
    {
        var types = GetNodeTypes(station.Function);
        return new GraphNode(station.Identifier.ToGuid(), station.Identifier, station.Position.X, station.Position.Y, 0f, types, null, null, null)
        {
            Theta = station.Position.Theta,
            TimeStamp = station.TimeStamp
        };
    }

    /// <summary>
    /// Convert station <see cref="Function"/> to <see cref="AgvGraph.NodeType"/> collection
    /// </summary>
    /// <param name="function"></param>
    /// <returns>The Mqtt.StationList.Station.function converted/mapped to AgvGraph.NodeType</returns>
    private static ICollection<AgvGraph.NodeType> GetNodeTypes(object function)
    {
        if (function is Function singleFunction)
        {
            return [ParseNodeType(singleFunction)];
        }
        return ((List<Function>)function).Select(ParseNodeType).ToList();
    }

    /// <summary>
    /// Converts station <see cref="Function"/> to <see cref="AgvGraph.NodeType"/>
    /// </summary>
    /// <param name="function"></param>
    /// <returns>The Mqtt.StationList.Station.function converted/mapped to AgvGraph.NodeType</returns>
    private static AgvGraph.NodeType ParseNodeType(Function function)
    {
        return function switch
        {
            Function.Both => AgvGraph.NodeType.PickDrop,
            Function.Charge => AgvGraph.NodeType.Charge,
            Function.Drop => AgvGraph.NodeType.Drop,
            Function.Park => AgvGraph.NodeType.Park,
            Function.Pick => AgvGraph.NodeType.Pick,
            Function.Maintenance => AgvGraph.NodeType.Maintenance,
            _ => AgvGraph.NodeType.PickDrop
        };
    }
}