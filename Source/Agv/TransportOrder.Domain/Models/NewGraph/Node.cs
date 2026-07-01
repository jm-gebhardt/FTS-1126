// ReSharper disable MissingBlankLines
// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public abstract class GraphResource
{
    public Guid Id { get; }  // unique identifier derived from LifId + nodeId/edgeId/stationId
    public string? Name { get; }
    public string? Description { get; }

    public List<RoutingResource> Children { get; } = [];
}

public abstract class RoutingResource
{
    public Guid Id { get; }  // unique identifier defined on creation

    public GraphResource Parent { get; } = null!;
    public List<RoutingResource> ConflictingResources { get; } = null!;
}

public class Node : GraphResource
{
    public string? MapId { get; }
    public double X { get; }
    public double Y { get; }

    //LevelId?

    public List<Edge> IncomingEdges { get; } = [];

    public List<Edge> OutgoingEdges { get; } = [];
}

public class Edge : GraphResource
{
    public Node StartNode { get; } = null!;
    public Node EndNode { get; } = null!;
}

public class Station : GraphResource
{
    public List<Node> InteractionNodes { get; } = [];

    public double? StationHeight { get; }

    public double X { get; }
    public double Y { get; }
    public double? Theta { get; }
}

public class RoutingNode : RoutingResource
{
    public double Theta { get; }
    public AgvType AgvType { get; } = null!;
    public LoadState LoadState { get; } = null!;

    //actions?
}

public class RoutingEdge : RoutingResource
{
    public RoutingNode StartNode { get; } = null!;
    public RoutingNode EndNode { get; } = null!;
    public AgvType AgvType { get; } = null!;
    public bool RotationAllowed { get; }
    public double? MaxSpeed { get; }
    public double? MaxRotationSpeed { get; }
    public Trajectory? Trajectory { get; } = null!;

    //actions?
}

public class AgvType
{
    // derived from Factsheet
}

public class LoadState
{
    // derived from Factsheet/loadSpecification
    // n:1 relation loadSets:LoadState
}

public class Trajectory
{
    // handels NURBS
}

public class Layout
{
    public string LayoutId { get; } = null!;

    public string? Name { get; }
    public string Version { get; } = null!;
    public string? LevelId { get; }
    public string? Description { get; }
    public List<Node> Nodes { get; } = [];
    public List<Edge> Edges { get; } = [];
    public List<Station> Stations { get; } = [];
}

public class LifFile
{
    public Guid Id { get; } // unique identifier defined on creation when "Add" API is called

    public MetaInformation MetaInformation { get; } = null!;

    public List<Layout> Layouts { get; } = [];

}

public class MetaInformation
{
    // see LIF documentation for details
}

public class GraphService
{
    public IReadOnlyCollection<Node> Nodes { get; } = [];
    public IReadOnlyCollection<Edge> Edges { get; } = [];
    public IReadOnlyCollection<Station> Stations { get; } = [];

    // methods to create, update, remove and query the graph resources. for example:

    public void AddLifFile(LifFile lifFile)
    {
        throw new InvalidOperationException();
    }

    public void RemoveLifFile(Guid lifFileId)
    {
        throw new InvalidOperationException();
    }

    public Node? GetNodeById(Guid nodeId)
    {
        throw new InvalidOperationException();
    }

    public Node? GetNodeByName(Guid nodeName)
    {
        throw new InvalidOperationException();
    }
}
