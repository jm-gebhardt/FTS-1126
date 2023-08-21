using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.Graphs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Resolvers;

/// <summary>
/// The custom value resolver, maps from AgvGraph and and the destination Graph to the Edges property
/// </summary>
public class GraphEdgeValueResolver : IValueResolver<AgvGraph, GraphDto, object>
{
    /// <summary>
    /// The resolve method
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="destMember"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public object Resolve(AgvGraph source, GraphDto destination, object destMember, ResolutionContext context)
    {
        var graphEdges = new List<GraphEdgeDto>();

        Dictionary<string, GraphNode> nodeDict = source.Nodes.ToDictionary(node => node.Id);

        foreach (var edge in source.Edges)
        {
            if (nodeDict.TryGetValue(edge.FromNodeId, out var fromNode) &&
                nodeDict.TryGetValue(edge.ToNodeId, out var toNode))
            {
                var fromNodeDto = context.Mapper.Map<GraphNodeDto>(fromNode);
                var toNodeDto = context.Mapper.Map<GraphNodeDto>(toNode);
                graphEdges.Add(new GraphEdgeDto {From = fromNodeDto, To = toNodeDto});
            }
        }

        return graphEdges;
    }
}