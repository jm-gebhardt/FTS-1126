using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles.Resolver;

/// <summary>
/// The custom value resolver.
/// Maps from AgvGraph to the GraphEdgeDtos.
/// </summary>
public class GraphEdgeValueResolver : IValueResolver<Graph, GraphDto, object>
{
    /// <inheritdoc />
    public object Resolve(Graph source, GraphDto destination, object destMember, ResolutionContext context)
    {
        var graphEdges = new List<GraphEdgeDto>();

        Dictionary<string, GraphNode> nodeDict = source.Nodes.ToDictionary(node => node.Id);

        foreach (var edge in source.Edges)
        {
            if (nodeDict.TryGetValue(edge.From.Id, out var fromNode) &&
                nodeDict.TryGetValue(edge.To.Id, out var toNode))
            {
                graphEdges.Add(new GraphEdgeDto
                {
                    CorrelationId = edge.CorrelationId,
                    FromNodeId = fromNode.Id, ToNodeId = toNode.Id,
                    Variant = edge.Variant, VariantParameters = edge.VariantParameters, Length = edge.Length
                });
            }
        }

        return graphEdges;
    }
}