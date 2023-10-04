using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using JetBrains.Annotations;
using MassTransit;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the GetAgv <see cref="GetAgvs"/>
/// Gets all Agv from AgvRegistry sorted by AgvId ascending, returns IEnumerable of Agv
/// </summary>
[UsedImplicitly]
public class GetAgvsRequestHandler : IConsumer<GetAgvs>,
    IConsumer<GetAgv>
{
    private readonly IAgvRegistry _agvRegistry;
    private readonly IActionGroupRetrievalService _actionGroupRegistry;
    private readonly IMapper _mapper;
    private readonly IAgvErrorRegistry _agvErrorRegistry;

    /// <summary>
    /// <inheritdoc cref="GetAgvsRequestHandler"/>
    /// </summary>
    /// <param name="agvRegistry"></param>
    /// <param name="actionGroupRegistry"></param>
    /// <param name="mapper"></param>
    /// <param name="agvErrorRegistry"></param>
    public GetAgvsRequestHandler(IAgvRegistry agvRegistry, IActionGroupRetrievalService actionGroupRegistry, IMapper mapper, IAgvErrorRegistry agvErrorRegistry)
    {
        _agvRegistry = agvRegistry;
        _actionGroupRegistry = actionGroupRegistry;
        _mapper = mapper;
        _agvErrorRegistry = agvErrorRegistry;
    }

    /// <inheritdoc cref="GetAgvsRequestHandler"/>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<GetAgvs> context)
    {
        IQueryable<IAgv> query = _agvRegistry.GetAllAgvs();
        ICollection<Guid> correlationIds = context.Message.CorrelationIds;
        if (correlationIds.Any())
        {
            query = query.Where(a => correlationIds.Contains(a.CorrelationId));
        }

        List<IAgv> agvs = query.OrderBy(a => a.AgvId).ToList();
        List<Guid> agvCorrelationIds = agvs.Select(a => a.CorrelationId).ToList();

        Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> actionGroupsTask = _actionGroupRegistry.GetAssignedActionGroupsWithNodesAsync(agvCorrelationIds);
        Task<IList<AgvError>> errorsTask = _agvErrorRegistry.GetActiveErrorsAsync(agvCorrelationIds);

        await Task.WhenAll(actionGroupsTask, errorsTask);

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
            return agvMessageContent;
        }).ToList();
        List<bool> areInitialState = agvsMessageContent.Select(a => a.CurrentState == AgvState.Initial).ToList();
        await context.RespondAsync(new GotAgvs(agvsMessageContent, areInitialState));
    }

    /// <summary>
    /// Handles the <see cref="GetAgv"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = _agvRegistry.GetAgv(agvCorrelationId);
        if (agv == null)
        {
            throw new AgvNotFoundException(agvCorrelationId);
        }

        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);

        var action = await _actionGroupRegistry.GetActionGroupWithNodesAsync(agvCorrelationId);
        _mapper.Map(action, agvMessageContent);

        IList<AgvError> agvErrors = await _agvErrorRegistry.GetActiveErrorsAsync(agvCorrelationId);
        _mapper.Map(agvErrors, agvMessageContent);
        await context.RespondAsync(new GotAgv(agvMessageContent));
    }
}