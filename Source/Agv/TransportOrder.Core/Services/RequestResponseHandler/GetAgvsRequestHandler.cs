using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
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
    private readonly IAgvRepository _agvRepository;
    private readonly IActionGroupRetrievalService _actionGroupRegistry;
    private readonly IMapper _mapper;
    private readonly IAgvErrorRegistry _agvErrorRegistry;

    /// <summary>
    /// Initializes a new  <see cref="GetAgvsRequestHandler"/> instance.
    /// </summary>
    public GetAgvsRequestHandler(IAgvRepository agvRepository, IActionGroupRetrievalService actionGroupRegistry, IMapper mapper, IAgvErrorRegistry agvErrorRegistry)
    {
        _agvRepository = agvRepository;
        _actionGroupRegistry = actionGroupRegistry;
        _mapper = mapper;
        _agvErrorRegistry = agvErrorRegistry;
    }

    /// <summary>
    /// Handles the <see cref="GetAgvs"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvs> context)
    {
        ICollection<Guid> correlationIds = context.Message.CorrelationIds;
        ICollection<Domain.Models.Agv.Agv> agvs;
        if (!correlationIds.Any())
        {
            agvs = await _agvRepository.GetAllAgvsAsync();
        }
        else
        {
            agvs = await _agvRepository.SelectExistingAgvsAsync(correlationIds.ToList());
        }

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

        var action = await _actionGroupRegistry.GetActionGroupWithNodesAsync(agvCorrelationId);
        _mapper.Map(action, agvMessageContent);

        IList<AgvError> agvErrors = await _agvErrorRegistry.GetActiveErrorsAsync(agvCorrelationId);
        _mapper.Map(agvErrors, agvMessageContent);
        await context.RespondAsync(new GotAgv(agvMessageContent));
    }
}