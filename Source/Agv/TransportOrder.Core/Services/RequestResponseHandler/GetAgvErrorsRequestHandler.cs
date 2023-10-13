using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for error-related requests.
/// </summary>
[UsedImplicitly]
public class GetAgvErrorsRequestHandler : 
    IConsumer<GetAgvErrors>,
    IConsumer<GetActiveAgvErrors>,
    IConsumer<GetErrorHistoryForAgvs>,
    IConsumer<GetActiveErrorsForAgvs>
{
    private readonly IAgvErrorRegistry _agvErrorRegistry;
    private readonly IMapper _mapper;
    private readonly IAgvRepository _agvRepository;

    /// <inheritdoc cref="GetAgvErrorsRequestHandler"/>
    public GetAgvErrorsRequestHandler(IAgvErrorRegistry agvErrorRegistry, IAgvRepository agvRepository, IMapper mapper)
    {
        _agvErrorRegistry = agvErrorRegistry;
        _agvRepository = agvRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Handle the <see cref="GetAgvErrors"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetAgvErrors> context)
    {
        var correlationId = context.Message.CorrelationId;
        await _agvRepository.GetAgvAsync(correlationId);
        IReadOnlyList<AgvError> agvErrorHistory = await _agvErrorRegistry.GetErrorsAsync(correlationId, context.Message.DateFrom, context.Message.DateTo);
        List<Messages.Requests.Agv.Queries.AgvError> agvErrorHistoryMessageContent = agvErrorHistory.Select(e => _mapper.Map<Messages.Requests.Agv.Queries.AgvError>(e)).ToList();
        await context.RespondAsync(new GotAgvErrors(agvErrorHistoryMessageContent));
    }

    /// <summary>
    /// Handle the <see cref="GetActiveAgvErrors"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetActiveAgvErrors> context)
    {
        var correlationId = context.Message.CorrelationId;
        await _agvRepository.GetAgvAsync(correlationId);
      
        IList<AgvError> agvErrors = await _agvErrorRegistry.GetActiveErrorsAsync(correlationId);
        List<Messages.Requests.Agv.Queries.AgvError> agvErrorsMessageContent = agvErrors.Select(e => _mapper.Map<Messages.Requests.Agv.Queries.AgvError>(e)).ToList();
        await context.RespondAsync(new GotActiveAgvErrors(agvErrorsMessageContent));
    }

    /// <summary>
    /// Handle the <see cref="GetErrorHistoryForAgvs"/> request.
    /// </summary>
    /// <exception cref="AgvsNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetErrorHistoryForAgvs> context)
    {
       (ICollection<Guid> selectedAgvs, var fromDate, var toDate) = context.Message;
        await CheckIfAgvsExist(selectedAgvs);
        IReadOnlyList<AgvError> agvErrorHistory = await _agvErrorRegistry.GetErrorsAsync(selectedAgvs.ToList(), fromDate, toDate);
        Dictionary<Guid, List<AgvError>> errorsByAgv = agvErrorHistory
            .GroupBy(e => e.AgvCorrelationId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.ErrorReceived).ToList());
        var errors = _mapper.Map<Dictionary<Guid, List<Messages.Requests.Agv.Queries.AgvError>>>(errorsByAgv);
        await context.RespondAsync(new GotErrorHistoryForAgvs(errors));
    }

    /// <summary>
    /// Handle the <see cref="GetActiveErrorsForAgvs"/> request.
    /// </summary>
    /// <exception cref="AgvsNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetActiveErrorsForAgvs> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
        await CheckIfAgvsExist(selectedAgvs);

        IList<AgvError> fetchedErrors = await _agvErrorRegistry.GetActiveErrorsAsync(selectedAgvs.ToList());
        Dictionary<Guid, List<AgvError>> errorsByAgv = fetchedErrors.GroupBy(e => e.AgvCorrelationId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.ErrorReceived).ToList());
        var errors = _mapper.Map<Dictionary<Guid, List<Messages.Requests.Agv.Queries.AgvError>>>(errorsByAgv);
        await context.RespondAsync(new GotActiveErrorsForAgvs(errors));
    }

    private async Task CheckIfAgvsExist(ICollection<Guid> selectedAgvs)
    {
        ICollection<Domain.Models.Agv.Agv> agvs = await _agvRepository.SelectExistingAgvsAsync(selectedAgvs.ToList());

        if (agvs.Count != selectedAgvs.Count)
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(agvs.Select(a => a.CorrelationId)).ToList());
        }
    }
}