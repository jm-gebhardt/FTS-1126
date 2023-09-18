using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using GraphEdge = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.GraphEdge;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.GraphNode;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;

/// <summary>
/// The custom value resolver, maps from AgvGraph and and the destination Graph to the Edges property
/// </summary>
public class GraphEdgeValueResolver : IValueResolver<AgvGraph, Graph, object>
{
    /// <summary>
    /// The resolve method
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="destMember"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public object Resolve(AgvGraph source, Graph destination, object destMember, ResolutionContext context)
    {
        var graphEdges = new List<GraphEdge>();

        Dictionary<string, GraphNode> nodeDict = source.Nodes.ToDictionary(node => node.Id);

        foreach (var edge in source.Edges)
        {
            if (nodeDict.TryGetValue(edge.FromNodeId, out var fromNode) &&
                nodeDict.TryGetValue(edge.ToNodeId, out var toNode))
            {
                var fromNodeDto = context.Mapper.Map<Requests.Agv.GraphNode>(fromNode);
                var toNodeDto = context.Mapper.Map<Requests.Agv.GraphNode>(toNode);
                graphEdges.Add(new GraphEdge(fromNodeDto, toNodeDto));
            }
        }

        return graphEdges;
    }
}