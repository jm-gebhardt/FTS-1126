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
    /// The node in the graph.
    /// </summary>
    public string? Node { get; set; }

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
    public Station(Guid id, string node, string source, string target, Guid groupId, int occupancy, Guid? agvCorrelationId)
    {
        Id = id;
        Node = node;
        Source = source;
        Target = target;
        GroupId = groupId;
        Occupancy = occupancy;
        AgvCorrelationId = agvCorrelationId;
    }

    /// <inheritdoc cref="Station"/>>
    public Station(string source, string target)
    {
        Id = source.ToGuid();
        Source = source;
        Target = target;
    }

    /// <inheritdoc cref="Station"/>>
    public Station()
    {
        Source = string.Empty;
        Target = string.Empty;
        Id = Source.ToGuid();
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
    /// <param name="node"></param>
    public void Localize(string node)
    {
        Node = node;
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