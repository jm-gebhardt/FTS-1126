using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the GetAgv <see cref="GetAgvs"/>
/// Gets all Agv from AgvRepository sorted by AgvId ascending, returns IEnumerable of Agv
/// </summary>
[UsedImplicitly]
public class GetAgvsRequestHandler : IConsumer<GetAgvs>,
    IConsumer<GetAgv>
{
    private readonly IAgvRepository _agvRepository;
    private readonly IActionGroupRetrievalService _actionGroupRepository;
    private readonly IMapper _mapper;
    private readonly IAgvErrorRepository _agvErrorRepository;
    private readonly IGraphRepository _agvGraphRepository;

    /// <summary>
    /// Initializes a new  <see cref="GetAgvsRequestHandler"/> instance.
    /// </summary>
    public GetAgvsRequestHandler(IAgvRepository agvRepository, IActionGroupRetrievalService actionGroupRepository, IMapper mapper, IAgvErrorRepository agvErrorRepository, IGraphRepository agvGraphRepository)
    {
        _agvRepository = agvRepository;
        _actionGroupRepository = actionGroupRepository;
        _mapper = mapper;
        _agvErrorRepository = agvErrorRepository;
        _agvGraphRepository = agvGraphRepository;
    }

    /// <summary>
    /// Handles the <see cref="GetAgvs"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvs> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
        ICollection<Domain.Models.Agv.Agv> agvs;
        if (!selectedAgvs.Any())
        {
            agvs = await _agvRepository.GetAllAgvsAsync();
        }
        else
        {
            agvs = await _agvRepository.SelectExistingAgvsAsync(selectedAgvs.ToList());
        }

        List<Guid> agvCorrelationIds = agvs.Select(a => a.CorrelationId).ToList();
        if (agvCorrelationIds.Count != selectedAgvs.Count && selectedAgvs.Any())
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(agvCorrelationIds).ToList());
        }

        Dictionary<Guid, string?> agvLastNodeIds = agvs.ToDictionary(a => a.CorrelationId, a => a.AgvStatus?.LastNodeId);
        
        Task<IReadOnlyDictionary<Guid, GraphNode?>> agvLastNodesTask = _agvGraphRepository.GetLastNodesForAgvsAsync(agvLastNodeIds);

        Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> actionGroupsTask = _actionGroupRepository.GetCurrentActionGroupsWithNodesAsync(agvCorrelationIds);
        Task<IList<AgvError>> errorsTask = _agvErrorRepository.GetActiveErrorsAsync(agvCorrelationIds);
        await Task.WhenAll(actionGroupsTask, errorsTask, agvLastNodesTask);

        Dictionary<Guid, List<AgvError>> allAgvErrors = errorsTask.Result
            .GroupBy(e => e.AgvCorrelationId)
            .ToDictionary(e => e.Key, e => e.ToList());

        List<Messages.MessageContent.Agv> agvsMessageContent = agvs.Select(agv =>
        {
            var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
            _mapper.Map(actionGroupsTask.Result[agv.CorrelationId], agvMessageContent);

            if (allAgvErrors.TryGetValue(agv.CorrelationId, out List<AgvError>? agvErrors) && agvErrors.Any())
            {
                _mapper.Map(agvErrors, agvMessageContent);
            }

            _mapper.Map(agvLastNodesTask.Result[agv.CorrelationId], agvMessageContent);

            return agvMessageContent;
        }).ToList();
        await context.RespondAsync(new GotAgvs(agvsMessageContent));
    }

    /// <summary>
    /// Handles the <see cref="GetAgv"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);

        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);

        var action = await _actionGroupRepository.GetCurrentAgvActionGroupWithNodesAsync(agvCorrelationId);
        _mapper.Map(action, agvMessageContent);

        IList<AgvError> agvErrors = await _agvErrorRepository.GetActiveErrorsAsync(agvCorrelationId);
        _mapper.Map(agvErrors, agvMessageContent);

        string? lastNodeId = agv.AgvStatus?.LastNodeId;
        if (lastNodeId != null)
        {
            var lastNode = await _agvGraphRepository.GetNodeAsync(lastNodeId);
            _mapper.Map(lastNode, agvMessageContent);
        }

        await context.RespondAsync(new GotAgv(agvMessageContent));
    }
}