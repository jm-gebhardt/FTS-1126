using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using TransportOrder.Messages.Requests.Agv;
using GraphNode = TransportOrder.Messages.Requests.Agv.GraphNode;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.Resolver;

/// <summary>
/// The custom value resolver, maps from AgvGraph and and the destination Graph to the Edges property
/// </summary>
public class GraphEdgeValueResolver : IValueResolver<Graph, GraphDto, object>
{
    /// <summary>
    /// The resolve method
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="destMember"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public object Resolve(Graph source, GraphDto destination, object destMember, ResolutionContext context)
    {
        var graphEdges = new List<GraphEdgeDto>();

        Dictionary<string, GraphNode> nodeDict = source.Nodes.ToDictionary(node => node.Id);

        foreach (var edge in source.Edges)
        {
            if (nodeDict.TryGetValue(edge.From.Id, out var fromNode) &&
                nodeDict.TryGetValue(edge.To.Id, out var toNode))
            {
                var fromNodeDto = context.Mapper.Map<GraphNodeDto>(fromNode);
                var toNodeDto = context.Mapper.Map<GraphNodeDto>(toNode);
                graphEdges.Add(new GraphEdgeDto {From = fromNodeDto, To = toNodeDto});
            }
        }

        return graphEdges;
    }
}