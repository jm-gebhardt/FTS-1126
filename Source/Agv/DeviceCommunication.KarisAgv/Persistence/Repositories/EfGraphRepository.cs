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
        return await context.Set<MqttNode>()
            .Where(n => graphNodeIds.Contains(n.NodeId))
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
        List<MqttEdgeMapping> edges = await context.Set<MqttEdgeMapping>()
            .Where(e => graphEdgeIds.Contains(e.InternalEdge))
            .AsNoTracking()
            .ToListAsync();
        return mapper.Map<ICollection<MqttEdge>>(edges);
    }

    /// <inheritdoc />
    public Task Add(string graphNodeId, MqttNode mqttNode)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
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
        });
    }

    /// <inheritdoc />
    public Task Add(Guid graphEdgeId, MqttEdge mqttEdge)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
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
        });
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