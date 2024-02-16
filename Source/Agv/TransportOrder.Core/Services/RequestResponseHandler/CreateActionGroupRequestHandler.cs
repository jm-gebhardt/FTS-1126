using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for action group creation requests.
/// </summary>
[UsedImplicitly]
public class CreateActionGroupRequestHandler :
    IConsumer<CreatePickDropActionGroup>
{
    private readonly IActionGroupRetrievalService _retrievalService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;
    private readonly IActionGroupValidationService _actionGroupValidationService;

    /// <inheritdoc cref="CreateActionGroupRequestHandler" />
    public CreateActionGroupRequestHandler(IActionGroupRetrievalService retrievalService,
        IActionGroupRepository actionGroupRepository,
        IActionGroupValidationService actionGroupValidationService,
        IAgvDb agvDb, IMapper mapper)
    {
        _retrievalService = retrievalService;
        _actionGroupRepository = actionGroupRepository;
        _agvDb = agvDb;
        _mapper = mapper;
        _actionGroupValidationService = actionGroupValidationService;
    }

    /// <summary>
    /// Handles the <see cref="Messages.Requests.ActionGroup.CreatePickDropActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<CreatePickDropActionGroup> context)
    {
        (string sourceNode, string targetNode, Guid? agvCorrelationId) = await ValidateMessageContent(context);

        var actionGroup = await CreatePickDropActionGroup(sourceNode, targetNode, agvCorrelationId);
        await _actionGroupRepository.AddAsync(actionGroup);

        await context.Publish(new Common.Messages.ActionGroups.Events.ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId));
        await context.RespondAsync(new ActionGroupCreated(await _retrievalService.EnrichActionGroupWithNodesAsync(actionGroup)));
    }

    private async Task<CreatePickDropActionGroup> ValidateMessageContent(ConsumeContext<CreatePickDropActionGroup> context)
    {
        (string source, string target, Guid? agvCorrelationId) = context.Message;

        // validate agv existence 
        if (agvCorrelationId != null)
        {
            await _agvDb.Agvs.ThrowIfAgvNotExistsAsync(agvCorrelationId.Value);
        }

        // validate source and target node
        var (sourceNode, targetNode) = await TryGetSourceAndTargetNode(source, target, agvCorrelationId);
        if (!sourceNode.IsPickStation())
        {
            throw new InvalidNodeTypeException(source, sourceNode.Type.ToString(), $"{nameof(AgvGraph.NodeType.Pick)} or {nameof(AgvGraph.NodeType.PickDrop)}");
        }

        if (!targetNode.IsDropStation())
        {
            throw new InvalidNodeTypeException(target, targetNode.Type.ToString(), $"{nameof(AgvGraph.NodeType.Drop)} or {nameof(AgvGraph.NodeType.PickDrop)}");
        }
        return context.Message;
    }

    private async Task<(GraphNode sourceNode, GraphNode targetNode)> TryGetSourceAndTargetNode(string source, string target, Guid? agvCorrelationId)
    {
        ICollection<GraphNode> nodes;
        if (agvCorrelationId == null)
        {
            // get source and target independent of agv graph 
            nodes = await _agvDb.Graphs.GetNodesByIdsAsync([source, target]);
        }
        else
        {
            // get source and target from the graph of the given agv
            nodes = (await _agvDb.Graphs.GetNodesAsync(agvCorrelationId.Value, node => new[] { source, target }.Contains(node.Id))).ToList();
        }

        return (nodes.SelectById(source), nodes.SelectById(target));
    }

    private async Task<Domain.Models.ActionGroup.ActionGroup> CreatePickDropActionGroup(string sourceNode, string targetNode, Guid? agvCorrelationId)
    {
        var correlationId = Guid.NewGuid();

        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            correlationId,
            $"M-{DateTime.UtcNow:yyyyMMdd-tthhmm}",
            actions: new List<Domain.Models.ActionGroup.AgvAction>
            {
                new(Guid.NewGuid(), ActionType.Pick, sourceNode, correlationId) { Sequence = 0 },
                new(Guid.NewGuid(), ActionType.Drop, targetNode, correlationId) { Sequence = 1 }
            },
            creator: ActionGroupCreator.TransportOrderWebApi,
            assignedAgv: agvCorrelationId
        );

        var validationResult = await _actionGroupValidationService.ValidateAsync(actionGroup);
        if (!validationResult.IsValid)
        {
            throw new ActionGroupInvalidException(validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        return actionGroup;
    }
}