using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using DomainAgvGraph = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.AgvGraph;
using DomainEdge = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.Edge;
using CommonAgvGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;

/// <inheritdoc />
[UsedImplicitly]
public class LifGraphConverter(
    INurbsEdgeBuilder nurbsEdgeBuilder,
    ILogger<LifGraphConverter> logger) : ILifGraphConverter
{
    /// <inheritdoc />
    public CommonAgvGraph ConvertToCommonAgvGraph(DomainAgvGraph domainGraph)
    {
        List<GraphNode> commonNodes = domainGraph.Nodes
            .Select(n => n.ToCommonGraphNode())
            .ToList();

        ApplyStationNodeTypes(domainGraph, commonNodes);

        var commonEdges = new List<GraphEdge>();
        foreach (var domainEdge in domainGraph.Edges)
        {
            var commonEdge = BuildCommonGraphEdge(domainGraph, domainEdge);
            commonEdges.Add(commonEdge);
        }

        return new CommonAgvGraph(domainGraph.AgvCorrelationId, commonNodes, commonEdges);
    }

    private void ApplyStationNodeTypes(DomainAgvGraph domainGraph, List<GraphNode> commonNodes)
    {
        foreach (var domainNode in domainGraph.Nodes)
        {
            if (domainNode.AgvActions.Count == 0)
            {
                continue;
            }

            ICollection<CommonAgvGraph.NodeType> nodeType = domainNode.AgvActions.InferNodeType();
            var commonNode = commonNodes.Single(n => n.Id == domainNode.Id);
            commonNode.UpdateType(nodeType);
            logger.LogDebug("Station type {NodeType} applied to node {NodeId}", nodeType, domainNode.Id);
        }
    }

    private GraphEdge BuildCommonGraphEdge(DomainAgvGraph domainGraph, DomainEdge domainEdge)
    {
        var fromNodeId = domainGraph.Nodes.Single(n => n.Id.ToString() == domainEdge.StartNodeId).Id;
        var toNodeId = domainGraph.Nodes.Single(n => n.Id.ToString() == domainEdge.EndNodeId).Id;
        var edgeId = domainEdge.Id;

        var nurbs = domainEdge.AgvTrajectory.ToNurbs()
                    ?? throw new InvalidOperationException($"No NURBS trajectory found for edge '{domainEdge.EdgeId}'");

        var graphEdge = nurbsEdgeBuilder.BuildFromNurbs(nurbs, fromNodeId, toNodeId, edgeId);
        graphEdge.TimeStamp = domainEdge.Timestamp;
        logger.LogDebug("Built edge {EdgeId} from NURBS with degree {Degree}", edgeId, nurbs.Degree);
        return graphEdge;
    }
}