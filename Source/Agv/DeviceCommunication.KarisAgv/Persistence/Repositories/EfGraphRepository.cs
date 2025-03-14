using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;

/// <summary>
/// Provides a repository for graph related queries.
/// </summary>
[UsedImplicitly]
public class EfGraphRepository(IDbContextFactory<DeviceCommunicationDbContext> dbContextFactory, IMapper mapper, IMqttEdgeMappingCacheProvider edgeCache, IMqttNodeCacheProvider nodeCache) : IGraphRepository
{
    /// <inheritdoc />
    public async Task<MqttNode?> GetMqttNode(string graphNodeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<MqttNode>().Where(n => n.NodeId == graphNodeId).SingleOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<ICollection<MqttNode>> GetMqttNodes(string[] graphNodeIds)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<MqttNode>().Where(n => graphNodeIds.Contains(n.NodeId)).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<MqttEdge?> GetMqttEdge(Guid graphEdgeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var edge = await context.Set<MqttEdgeMapping>().Where(e => e.InternalEdge == graphEdgeId).SingleOrDefaultAsync();
        return mapper.Map<MqttEdge?>(edge);
    }

    /// <inheritdoc />
    public async Task<ICollection<MqttEdge>> GetMqttEdges(Guid[] graphEdgeIds)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        List<MqttEdgeMapping> edges = await context.Set<MqttEdgeMapping>().Where(e => graphEdgeIds.Contains(e.InternalEdge)).ToListAsync();
        return mapper.Map<ICollection<MqttEdge>>(edges);
    }

    /// <inheritdoc />
    public async Task Add(string graphNodeId, MqttNode mqttNode)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        if (nodeCache.ConcurrentDict.ContainsKey(graphNodeId) || await context.Set<MqttNode>().AnyAsync(n => n.NodeId == graphNodeId))
        {
            return;
        }
        if (nodeCache.ConcurrentDict.TryAdd(graphNodeId, mqttNode.NodeId))
        {
            context.Set<MqttNode>().Add(mqttNode);
            await context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task Add(Guid graphEdgeId, MqttEdge mqttEdge)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        if (edgeCache.ConcurrentDict.ContainsKey(graphEdgeId) || await context.Set<MqttEdgeMapping>().AnyAsync(e => e.InternalEdge == graphEdgeId))
        {
            return;
        }
        var edgeMapping = new MqttEdgeMapping(graphEdgeId)
        {
            Attributes = mqttEdge.Attributes,
            Bidirectional = mqttEdge.Bidirectional,
            ControlPoints = mqttEdge.ControlPoints,
            EdgeId = mqttEdge.EdgeId,
            EndId = mqttEdge.EndId,
            EdgeType = mqttEdge.EdgeType,
            ExitAngle = mqttEdge.ExitAngle,
            ExitDistance = mqttEdge.ExitDistance,
            MaxVelocity = mqttEdge.MaxVelocity,
            StartId = mqttEdge.StartId
        };
        if (edgeCache.ConcurrentDict.TryAdd(graphEdgeId, mqttEdge.EdgeId))
        {
            context.Set<MqttEdgeMapping>().Add(edgeMapping);
            await context.SaveChangesAsync();
        }
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