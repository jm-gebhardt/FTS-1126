using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents a repository for managing graph resources.
/// </summary>
[UsedImplicitly]
public class GraphRepository
{
    // methods to create, update, remove and query the graph resources. for example:
    /// <summary>
    /// 
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [UsedImplicitly]
    public Node GetNodeById(Guid nodeId)
    {
        throw new InvalidOperationException();
    }

    ///
    [UsedImplicitly]
    public Node GetNodeByName(string nodeName)
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [UsedImplicitly]
    public Station[] GetChargingStations()
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [UsedImplicitly]
    public string[] GetLevelIds()
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="levelId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [UsedImplicitly]
    public (Node[] Nodes, Edge[] Edges, Station[] Stations) GetLevel([UsedImplicitly]string levelId)
    {
        throw new InvalidOperationException();
    }
}