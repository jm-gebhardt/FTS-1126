using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Models.Graphs.GraphNode;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;

/// <summary>
/// The custom value resolver, maps from IGraph and the destination Graph to the GraphEdge property
/// </summary>
[UsedImplicitly]
public class GraphGraphEdgeResolver : IValueResolver<IGraph, Graph, object>
{
    /// <summary>
    /// The resolve method
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="destMember"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public object Resolve(IGraph source, Graph destination, object destMember, ResolutionContext context)
    {
        var graphEdges = new List<GraphEdge>();

        Dictionary<string, GraphNode> nodeDict = source.Nodes.ToDictionary(node => node.Id);

        foreach (var edge in source.Edges)
        {
            if (nodeDict.TryGetValue(edge.FromNodeId, out var fromNode) &&
                nodeDict.TryGetValue(edge.ToNodeId, out var toNode))
            {
                var fromNodeDto = context.Mapper.Map<MessageContent.GraphNode>(fromNode);
                var toNodeDto = context.Mapper.Map<MessageContent.GraphNode>(toNode);
                graphEdges.Add(new GraphEdge(fromNodeDto, toNodeDto, edge.Variant, edge.VariantParameters, edge.Length));
            }
        }

        return graphEdges;
    }
}