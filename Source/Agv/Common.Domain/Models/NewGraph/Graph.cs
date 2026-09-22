
namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents the global graph consisting of nodes, edges and stations.
/// </summary>
public record Graph(IReadOnlyCollection<Node> Nodes, IReadOnlyCollection<Edge> Edges, IReadOnlyCollection<Station> Stations) : GraphSegment(Nodes, Edges, Stations);