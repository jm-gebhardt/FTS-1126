using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles requests for bounding box conflicts between graph resources.
/// </summary>
[UsedImplicitly]
public class BoundingBoxConflictRequestHandler
    : IConsumer<GetBoundingBoxConflicts>, IConsumer<GetNodeBoundingBoxConflicts>, IConsumer<GetEdgeBoundingBoxConflicts>
{
    private readonly IRoutingResourceConflictRepository _conflictRepository;
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="BoundingBoxConflictRequestHandler"/>
    public BoundingBoxConflictRequestHandler(IRoutingResourceConflictRepository conflictRepository, IAgvDb agvDb, IMapper mapper)
    {
        _conflictRepository = conflictRepository;
        _agvDb = agvDb;
        _mapper = mapper;
    }

    private async Task RespondWithConflictsAsync(ConsumeContext context, IReadOnlyList<ResourceConflict> conflicts)
    {
        var content = _mapper.Map<IReadOnlyList<BoundingBoxConflictContent>>(conflicts);
        await context.RespondAsync(new GotBoundingBoxConflicts(content));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetBoundingBoxConflicts> context)
    {
        await RespondWithConflictsAsync(context, await _conflictRepository.GetAllAsync());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetNodeBoundingBoxConflicts> context)
    {
        var nodeId = context.Message.NodeId;
        if (!await _agvDb.NewGraphs.RoutingNodeExistsAsync(nodeId))
        {
            throw new NodeNotFoundException(nodeId);
        }

        await RespondWithConflictsAsync(context, await _conflictRepository.GetByResourceAsync(nodeId));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetEdgeBoundingBoxConflicts> context)
    {
        var edgeId = context.Message.EdgeId;
        if (!await _agvDb.NewGraphs.RoutingEdgeExistsAsync(edgeId))
        {
            throw new EdgeNotFoundException(edgeId);
        }

        await RespondWithConflictsAsync(context, await _conflictRepository.GetByResourceAsync(edgeId));
    }
}