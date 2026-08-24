using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a graph consisting of nodes, edges and stations.
/// </summary>
/// <param name="Nodes">The nodes of the graph.</param>
/// <param name="Edges">The edges connecting the nodes.</param>
/// <param name="Stations">The stations of the graph.</param>
[PublicAPI]
public record Graph(IReadOnlyCollection<Node> Nodes, IReadOnlyCollection<Edge> Edges, IReadOnlyCollection<Station> Stations);