using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using JetBrains.Annotations;
using MassTransit;
using CommonGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.Graph;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles requests for bounding box conflicts between graph resources.
/// </summary>
[UsedImplicitly]
public class BoundingBoxConflictRequestHandler
    : IConsumer<GetBoundingBoxConflicts>, IConsumer<GetNodeBoundingBoxConflicts>, IConsumer<GetEdgeBoundingBoxConflicts>
{
    private readonly IBoundingBoxRepository _boundingBoxRepository;
    private readonly IAgvDb _agvDb;

    /// <inheritdoc cref="BoundingBoxConflictRequestHandler"/>
    public BoundingBoxConflictRequestHandler(IBoundingBoxRepository boundingBoxRepository, IAgvDb agvDb)
    {
        _boundingBoxRepository = boundingBoxRepository;
        _agvDb = agvDb;
    }

    private async Task<IReadOnlyList<BoundingBoxConflictContent>> GetConflictsAsync(CommonGraph graph, Guid? resourceId)
    {
        ICollection<Domain.Models.Agv.Agv> allAgvs = await _agvDb.Agvs.GetAllAgvsAsync();
        List<Guid> correlationIds = allAgvs.Select(a => a.CorrelationId).ToList();
        ICollection<AgvData> agvDatas = await _agvDb.Data.GetSelectedAgvDataAsync(correlationIds);

        string? worstCaseType = agvDatas.MaxBy(d => d.Length + d.Width)?.Type;
        if (worstCaseType is null)
        {
            return [];
        }

        IReadOnlyList<ResourceBoundingBox> boxes = await _boundingBoxRepository.GetByAgvTypeAsync(worstCaseType);
        var detector = new BoundingBoxConflictDetector(boxes, graph);
        IReadOnlyList<BoundingBoxConflict> conflicts = resourceId is null ? detector.Detect() : detector.DetectFor(resourceId.Value);

        return conflicts
            .Select(c => new BoundingBoxConflictContent(c.ResourceA.ResourceId, c.ResourceB.ResourceId))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetBoundingBoxConflicts> context)
    {
        var graph = await _agvDb.Graphs.GetGraphAsync();
        IReadOnlyList<BoundingBoxConflictContent> content = await GetConflictsAsync(graph, null);
        await context.RespondAsync(new GotBoundingBoxConflicts(content));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetNodeBoundingBoxConflicts> context)
    {
        var nodeId = context.Message.NodeId;
        var graph = await _agvDb.Graphs.GetGraphAsync();
        if (graph.Nodes.All(n => n.Id != nodeId))
        {
            throw new NodeNotFoundException(nodeId);
        }

        IReadOnlyList<BoundingBoxConflictContent> content = await GetConflictsAsync(graph, nodeId);
        await context.RespondAsync(new GotBoundingBoxConflicts(content));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetEdgeBoundingBoxConflicts> context)
    {
        var edgeId = context.Message.EdgeId;
        var graph = await _agvDb.Graphs.GetGraphAsync();
        if (graph.Edges.All(e => e.Id != edgeId))
        {
            throw new EdgeNotFoundException(edgeId);
        }

        IReadOnlyList<BoundingBoxConflictContent> content = await GetConflictsAsync(graph, edgeId);
        await context.RespondAsync(new GotBoundingBoxConflicts(content));
    }
}