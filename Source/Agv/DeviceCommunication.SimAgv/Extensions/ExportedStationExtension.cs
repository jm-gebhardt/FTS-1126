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
        object type = GetNodeType(station.Function);
        bool isNodeType = type is AgvGraph.NodeType;
        if (isNodeType)
        {
            return new GraphNode(station.Identifier.ToGuid(), station.Identifier, station.Position.X, -station.Position.Y, 0f, (AgvGraph.NodeType) type, null, null, null)
            {
                Theta = station.Position.Theta,
                TimeStamp = station.TimeStamp
            };
        }
        return new GraphNode(station.Identifier.ToGuid(), station.Identifier, station.Position.X, -station.Position.Y, 0f, (List<AgvGraph.NodeType>) type, null, null, null)
        {
            Theta = station.Position.Theta,
            TimeStamp = station.TimeStamp
        };
    }

    /// <summary>
    /// Convert station <see cref="Function"/> to <see cref="AgvGraph.NodeType"/>
    /// </summary>
    /// <param name="function"></param>
    /// <returns>The Mqtt.StationList.Station.function converted/mapped to AgvGraph.NodeType</returns>
    private static object GetNodeType(object function)
    {
        bool isFunction = function is Function;
        if (isFunction)
        {
            return ParseNodeType((Function) function);
        }
        return ((List<Function>) function).Select(ParseNodeType).ToList();
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