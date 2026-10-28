using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Agvs;

/// <summary>
/// Provider for AGV information. Holds relevant information <see cref="RoutingAgv"/> in-memory and updates it based on incoming events.
/// </summary>
[UsedImplicitly]
internal class AgvProvider(ILogger<AgvProvider> logger, IAgvRepository agvRepository, GraphProvider graphProvider)
{
    private readonly Dictionary<Guid, RoutingAgv> _agvs = [];

    public bool TryGetAgv(Guid agvCorrelationId, [NotNullWhen(true)] out RoutingAgv? value)
    {
        return _agvs.TryGetValue(agvCorrelationId, out value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var initializationWatch = Stopwatch.StartNew();
        
        var agvs = await agvRepository.GetAllAgvsAsync(cancellationToken);

        foreach (var agv in agvs)
        {
            if (_agvs.ContainsKey(agv.CorrelationId))
            {
                continue;
            }

            if (agv.AgvTypeId is null || !graphProvider.TryGetAgvType(agv.AgvTypeId.Value, out var agvType))
            {
                logger.LogWarning("Ignoring AGV {AgvCorrelationId} with unknown AgvTypeId '{AgvTypeId}'", agv.CorrelationId, agv.AgvTypeId);
                continue;
            }

            var routingAgv = new RoutingAgv(agv.CorrelationId, agvType, null, false, false, false);

            _agvs.Add(agv.CorrelationId, routingAgv);
        }

        initializationWatch.Stop();
        logger.LogDebug("{ClassName} initialized in {ElapsedMilliseconds} ms", nameof(AgvProvider), initializationWatch.ElapsedMilliseconds);
    }

    public void UpdateAgvStatus(AgvTrafficManagementStatusReceived receivedStatus)
    {
        if (!TryGetAgv(receivedStatus.AgvId, out var agv))
        {
            logger.LogWarning("Ignored status for unknown AGV {AgvCorrelationId}", receivedStatus.AgvId);
            return;
        }

        bool wasUpdated = false;

        if (agv.IsAutomatic != receivedStatus.IsAutomatic)
        {
            agv.IsAutomatic = receivedStatus.IsAutomatic;
            wasUpdated = true;
        }

        if (agv.HasFatalError != receivedStatus.HasFatalError)
        {
            agv.HasFatalError = receivedStatus.HasFatalError;
            wasUpdated = true;
        }

        if (agv.IsIdle != receivedStatus.IsIdle)
        {
            agv.IsIdle = receivedStatus.IsIdle;
            wasUpdated = true;
        }

        if (agv.IsIdle) // update routing node only if AGV is idle, otherwise the routing node is updated by the completion of the route steps
        {
            var routingNode = GetRoutingNode(agv, receivedStatus);

            if (agv.CurrentRoutingNode != routingNode)
            {
                agv.CurrentRoutingNode = routingNode;
                wasUpdated = true;
            }
        }

        if (wasUpdated)
        {
            logger.LogInformation("State updated: {AgvState}", agv);
        }
    }

    private RoutingNode? GetRoutingNode(RoutingAgv agv, AgvTrafficManagementStatusReceived receivedStatus)
    {
        if (!receivedStatus.IsPositionInitialized)
        {
            return null;
        }

        double theta = receivedStatus.Theta;

        var parentNodeCandidates = TrafficManagementConfig.UseLastNodeIdForNodeFinding ? GetParentNodeCandidatesFromLastNodeId(receivedStatus)
                                                                                                 : GetParentNodeCandidatesFromXy(agv, receivedStatus);

        foreach (var parentNodeCandidate in parentNodeCandidates)
        {
            var sortedRoutingNodes = parentNodeCandidate.RoutingNodes.Where(rn => rn.AgvType.Id == agv.AgvType.Id)
                                                                                                .Where(rn => rn.LoadState.IsLoaded == receivedStatus.IsLoaded)
                                                                                                .OrderBy(rn => rn != agv.CurrentRoutingNode)
                                                                                                .ThenBy(rn => Orientation.GetOrientationDifference(theta, rn.Theta));
            foreach (var routingNode in sortedRoutingNodes)
            {
                double orientationDeviation = routingNode == agv.CurrentRoutingNode ? TrafficManagementConfig.MaxOrientationDeviationToKeepNode 
                                                                                    : TrafficManagementConfig.MaxOrientationDeviationToFindNode;

                if (Orientation.GetOrientationDifference(theta, routingNode.Theta) <= orientationDeviation)
                {
                    return routingNode;
                }
            }
        }

        return null;
    }

    private List<Node> GetParentNodeCandidatesFromLastNodeId(AgvTrafficManagementStatusReceived receivedStatus)
    {
        if (receivedStatus.LastLifNodeId is null)
        {
            return [];
        }
            
        var nodes = graphProvider.Nodes.Where(n => n.LifNodeId == receivedStatus.LastLifNodeId && n.MapId == receivedStatus.MapId).ToList(); // Performance improvement Dictionary (MapId, LifNodeId) → Node im GraphProvider

        if (nodes.Count > 1)
        {
            logger.LogError("Multiple nodes found with lifNodeId '{LastLifNodeId}' and mapId '{MapId}': {Nodes}", receivedStatus.LastLifNodeId, receivedStatus.MapId, string.Join(", ", nodes.Select(n => n.Id)));
        }
            
        if (nodes.Count == 0)
        {
            logger.LogWarning("Node with lifNodeId '{LastLifNodeId}' and mapId '{MapId}' not found", receivedStatus.LastLifNodeId, receivedStatus.MapId);
            return [];
        }

        return nodes;
    }
    
    private List<Node> GetParentNodeCandidatesFromXy(RoutingAgv agv, AgvTrafficManagementStatusReceived receivedStatus)
    {
        double x = receivedStatus.X;
        double y = receivedStatus.Y;
        var currentParentNode = agv.CurrentRoutingNode?.ParentNode;

        if (currentParentNode is not null)
        {
            if (currentParentNode.MapId == receivedStatus.MapId && GetDistance(currentParentNode, x, y) <= TrafficManagementConfig.MaxDistanceToKeepNode)
            {
                return [currentParentNode];
            }
        }

        var nodeCandidates = graphProvider.Nodes.Where(n => n.MapId == receivedStatus.MapId)
                                                                .Where(n => GetDistance(n, x, y) <= TrafficManagementConfig.MaxDistanceToFindNode); // Performance improvement with spacial Index possible

        return nodeCandidates.OrderBy(n => GetDistance(n, x, y)).ToList();
    }

    private static double GetDistance(Node node, double x, double y)
    {
        double dx = node.X - x;
        double dy = node.Y - y;

        return Math.Sqrt(dx * dx + dy * dy);
    }
}