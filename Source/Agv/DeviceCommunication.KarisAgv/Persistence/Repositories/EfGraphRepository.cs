using AutoMapper;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.SemaphoreProvider;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;

/// <summary>
/// Provides a repository for graph related queries.
/// </summary>
[UsedImplicitly]
public class EfGraphRepository(
    IDbContextFactory<DeviceCommunicationDbContext> dbContextFactory,
    IMapper mapper,
    IMqttEdgeMappingCacheProvider edgeCache,
    IMqttNodeCacheProvider nodeCache,
    ISemaphoreProvider<IGraphRepositorySemaphore> semaphoreProvider
) : IGraphRepository
{
    /// <inheritdoc />
    public async Task<MqttNode?> GetMqttNode(string graphNodeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<MqttNode>()
            .Where(n => n.NodeId == graphNodeId)
            .AsNoTracking()
            .SingleOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<ICollection<MqttNode>> GetMqttNodes(string[] graphNodeIds)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        // Cast to IEnumerable to avoid C# 14 span overload resolution breaking change
        IEnumerable<string> nodeIds = graphNodeIds;
        return await context.Set<MqttNode>()
            .Where(n => nodeIds.Contains(n.NodeId))
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<MqttEdge?> GetMqttEdge(Guid graphEdgeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var edge = await context.Set<MqttEdgeMapping>()
            .Where(e => e.InternalEdge == graphEdgeId)
            .AsNoTracking()
            .SingleOrDefaultAsync();
        return mapper.Map<MqttEdge?>(edge);
    }

    /// <inheritdoc />
    public async Task<ICollection<MqttEdge>> GetMqttEdges(Guid[] graphEdgeIds)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        // Cast to IEnumerable to avoid C# 14 span overload resolution breaking change
        IEnumerable<Guid> edgeIds = graphEdgeIds;
        var edges = await context.Set<MqttEdgeMapping>()
            .Where(e => edgeIds.Contains(e.InternalEdge))
            .AsNoTracking()
            .ToListAsync();
        return mapper.Map<ICollection<MqttEdge>>(edges);
    }

    /// <inheritdoc />
    public Task Add(string graphNodeId, MqttNode mqttNode) => Add((graphNodeId, mqttNode));

    private Task Add(params (string graphNodeId, MqttNode mqttNode)[] nodes)
    {
        if (nodes.Length == 0)
        {
            throw new ArgumentException("Nodes cannot be empty.", nameof(nodes));
        }

        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var nodesToAdd = nodes.Where(n => !nodeCache.ConcurrentDict.ContainsKey(n.graphNodeId))
                .Select(n => n.mqttNode).ToList();

            if (nodesToAdd.Count == 0)
            {
                return;
            }

            context.Set<MqttNode>().AddRange(nodesToAdd);
            await context.SaveChangesAsync();

            foreach (var node in nodesToAdd)
            {
                nodeCache.ConcurrentDict[node.NodeId] = node.NodeId;
            }
        });
    }

    /// <inheritdoc />
    public Task Add(Guid graphEdgeId, MqttEdge mqttEdge) => Add((graphEdgeId, mqttEdge));

    /// <inheritdoc />
    public Task Add(params (Guid GraphEdgeId, MqttEdge MqttEdge)[] edges)
    {
        if (edges.Length == 0)
        {
            throw new ArgumentException("Edges cannot be empty.", nameof(edges));
        }

        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var edgesToAdd = edges.Where(e => !edgeCache.ConcurrentDict.ContainsKey(e.GraphEdgeId))
                .Select(e => (e.GraphEdgeId, e.MqttEdge, Mapping: new MqttEdgeMapping(e.GraphEdgeId)
                {
                    Attributes = e.MqttEdge.Attributes,
                    Bidirectional = e.MqttEdge.Bidirectional,
                    ControlPoints = e.MqttEdge.ControlPoints,
                    EdgeId = e.MqttEdge.EdgeId,
                    EndId = e.MqttEdge.EndId,
                    EdgeType = e.MqttEdge.EdgeType,
                    ExitAngle = e.MqttEdge.ExitAngle,
                    ExitDistance = e.MqttEdge.ExitDistance,
                    MaxVelocity = e.MqttEdge.MaxVelocity,
                    StartId = e.MqttEdge.StartId
                })).ToList();

            if (edgesToAdd.Count == 0)
            {
                return;
            }

            context.Set<MqttEdgeMapping>().AddRange(edgesToAdd.Select(e => e.Mapping));
            await context.SaveChangesAsync();

            foreach (var (graphEdgeId, mqttEdge, _) in edgesToAdd)
            {
                edgeCache.ConcurrentDict[graphEdgeId] = mqttEdge.EdgeId;
            }
        });
    }

    /// <inheritdoc />
    public async Task Add(Dictionary<string, MqttNode> nodesById, Dictionary<Guid, MqttEdge> edgesById)
    {
        await Add(nodesById.Select(n => (n.Key, n.Value)).ToArray());
        await Add(edgesById.Select(e => (e.Key, e.Value)).ToArray());
    }

    /// <inheritdoc />
    public async Task LoadCache()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        foreach (var edge in await context.Set<MqttEdgeMapping>().ToListAsync())
        {
            edgeCache.ConcurrentDict.TryAdd(edge.InternalEdge, edge.EdgeId);
        }

        foreach (var node in await context.Set<MqttNode>().ToListAsync())
        {
            nodeCache.ConcurrentDict.TryAdd(node.NodeId, node.NodeId);
        }
    }
}