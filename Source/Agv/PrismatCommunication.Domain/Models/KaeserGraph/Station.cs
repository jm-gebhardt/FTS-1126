using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

/// <summary>
/// A station.
/// </summary>
[PublicAPI]
public class Station
{
    /// <summary>
    /// The id of the station;
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The node id in the graph.
    /// </summary>
    public Guid NodeId { get; private set; }

    /// <summary>
    /// The node name in the graph.
    /// </summary>
    public string NodeName { get; private set; }

    /// <summary>
    /// The prismat code used to identify the source.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// The prismat code used to identify the target.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// The station group id.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// the agv occupying the station.
    /// </summary>
    public Guid? AgvCorrelationId { get; private set; }

    /// <summary>
    /// The occupancy of the station.
    /// </summary>
    public int Occupancy { get; private set; }

    /// <inheritdoc cref="Station"/>>
    [JsonConstructor]
    public Station(Guid id, Guid nodeId, string nodeName, string source, string target, Guid groupId, int occupancy, Guid? agvCorrelationId)
    {
        Id = id;
        NodeId = nodeId;
        NodeName = nodeName;
        Source = source;
        Target = target;
        GroupId = groupId;
        Occupancy = occupancy;
        AgvCorrelationId = agvCorrelationId;
    }

    /// <inheritdoc cref="Station"/>>
    public Station(string source, string target, Guid nodeId, string nodeName)
    {
        Id = source.ToGuid();
        Source = source;
        Target = target;
        NodeId = nodeId;
        NodeName = nodeName;
    }

    /// <summary>
    /// Assign to group.
    /// </summary>
    /// <param name="groupId"></param>
    public void AssignToGroup(Guid groupId)
    {
        GroupId = groupId;
    }

    /// <summary>
    /// Localize the station.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <param name="nodeName">The node name.</param>
    public void Localize(Guid nodeId, string nodeName)
    {
        NodeId = nodeId;
        NodeName = nodeName;
    }

    /// <summary>
    /// Add agv to station.
    /// </summary>
    public void AddAgv(Guid agvCorrelationId)
    {
        if (AgvCorrelationId == agvCorrelationId)
        {
            return;
        }
        // Only increase occupancy for unoccupied stations,
        // as for target selection the actual number of vehicles is irrelevant.
        if (Occupancy < 1)
        {
            Occupancy += 1;
        }
        AgvCorrelationId = agvCorrelationId;
    }

    /// <summary>
    /// Remove agv from station.
    /// </summary>
    public void RemoveAgv()
    {
        AgvCorrelationId = null;
        if (Occupancy == 0)
        {
            return;
        }
        Occupancy -= 1;
    }
}