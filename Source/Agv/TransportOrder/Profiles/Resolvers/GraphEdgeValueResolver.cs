using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Profiles.Resolvers
{
    /// <summary>
    /// The custom value resolver, maps from AgvGraph and and the destination Graph to the Edges property
    /// </summary>
    public class GraphEdgeValueResolver : IValueResolver<AgvGraph,Graph, object>
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
            var graphEdges = new List<Graph.Edge>();

            Dictionary<string, AgvGraph.Node> nodeDict = source.Nodes.ToDictionary(node => node.Id);

            foreach (var edge in source.Edges)
            {
                if (nodeDict.TryGetValue(edge.From, out var fromString) &&
                    nodeDict.TryGetValue(edge.To, out var toString))
                {
                    graphEdges.Add(new(fromString, toString));
                }
            }

            return graphEdges;
        }
    }
}
