using AutoMapper;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

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
    private readonly IMapper _mapper;
    private readonly IAgvDb _agvDb;

    /// <inheritdoc cref="GetAgvErrorsRequestHandler"/>
    public GetAgvErrorsRequestHandler(IAgvDb agvDb, IMapper mapper)
    {
        _agvDb = agvDb;
        _mapper = mapper;
    }

    /// <summary>
    /// Handle the <see cref="GetAgvErrors"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetAgvErrors> context)
    {
        var correlationId = context.Message.CorrelationId;
        await _agvDb.Agvs.GetAgvAsync(correlationId, context.CancellationToken);
        IReadOnlyList<AgvError> agvErrorHistory = await _agvDb.Errors.GetErrorsAsync(correlationId, context.Message.DateFrom, context.Message.DateTo, context.CancellationToken);
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
        await _agvDb.Agvs.GetAgvAsync(correlationId, context.CancellationToken);

        IReadOnlyList<AgvError> agvErrors = await _agvDb.Errors.GetActiveErrorsAsync(correlationId);
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
        await _agvDb.Agvs.ThrowIfAgvsNotExistAsync(selectedAgvs, context.CancellationToken);
        IReadOnlyList<AgvError> agvErrorHistory = await _agvDb.Errors.GetErrorsAsync(selectedAgvs.ToList(), fromDate, toDate, context.CancellationToken);
        List<AgvErrors> agvErrors = agvErrorHistory
            .GroupBy(e => e.AgvCorrelationId)
            .Select(g => new AgvErrors(
                    g.Key,
                    _mapper.Map<List<Messages.Requests.Agv.Queries.AgvError>>(
                        g.OrderByDescending(e => e.ErrorReceived).ToList())
                )
            )
            .ToList();

        await context.RespondAsync(new GotErrorHistoryForAgvs(agvErrors));
    }

    /// <summary>
    /// Handle the <see cref="GetActiveErrorsForAgvs"/> request.
    /// </summary>
    /// <exception cref="AgvsNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetActiveErrorsForAgvs> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;
        await _agvDb.Agvs.ThrowIfAgvsNotExistAsync(selectedAgvs, context.CancellationToken);

        IReadOnlyList<AgvError> fetchedErrors = await _agvDb.Errors.GetActiveErrorsAsync([.. selectedAgvs]);
        List<AgvErrors> errors = fetchedErrors
            .GroupBy(e => e.AgvCorrelationId)
            .Select(g => new AgvErrors(
                    g.Key,
                    _mapper.Map<List<Messages.Requests.Agv.Queries.AgvError>>(
                        g.OrderByDescending(e => e.ErrorReceived).ToList())
                )
            )
            .ToList();

        await context.RespondAsync(new GotActiveErrorsForAgvs(errors));
    }
}