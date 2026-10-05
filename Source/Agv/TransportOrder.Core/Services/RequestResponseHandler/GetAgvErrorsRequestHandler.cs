using AutoMapper;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using JetBrains.Annotations;
using MassTransit;

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
        var cancellationToken = context.CancellationToken;

        await _agvDb.Agvs.GetAgvAsync(correlationId, cancellationToken);
        var agvErrorHistory = await _agvDb.Errors.GetErrorsAsync(correlationId, context.Message.DateFrom, context.Message.DateTo, cancellationToken);
        var agvErrorHistoryMessageContent = agvErrorHistory.Select(e => _mapper.Map<AgvError>(e)).ToList();
        await context.RespondAsync(new GotAgvErrors(agvErrorHistoryMessageContent));
    }

    /// <summary>
    /// Handle the <see cref="GetActiveAgvErrors"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetActiveAgvErrors> context)
    {
        var correlationId = context.Message.CorrelationId;
        await _agvDb.Agvs.GetAgvAsync(correlationId);

        var agvErrors = await _agvDb.Errors.GetActiveErrorsAsync(correlationId);
        var agvErrorsMessageContent = agvErrors.Select(e => _mapper.Map<AgvError>(e)).ToList();
        await context.RespondAsync(new GotActiveAgvErrors(agvErrorsMessageContent));
    }

    /// <summary>
    /// Handle the <see cref="GetErrorHistoryForAgvs"/> request.
    /// </summary>
    /// <exception cref="AgvsNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetErrorHistoryForAgvs> context)
    {
        var (selectedAgvs, fromDate, toDate) = context.Message;
        var cancellationToken = context.CancellationToken;
        
        await _agvDb.Agvs.ThrowIfAgvsNotExistAsync(selectedAgvs);
        var agvErrorHistory = await _agvDb.Errors.GetErrorsAsync(selectedAgvs.ToList(), fromDate, toDate, cancellationToken);
        var agvErrors = agvErrorHistory
            .GroupBy(e => e.AgvCorrelationId)
            .Select(g => new AgvErrors(
                    g.Key,
                    _mapper.Map<List<AgvError>>(
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
        var selectedAgvs = context.Message.CorrelationIds;
        await _agvDb.Agvs.ThrowIfAgvsNotExistAsync(selectedAgvs);

        var fetchedErrors = await _agvDb.Errors.GetActiveErrorsAsync([.. selectedAgvs]);
        var errors = fetchedErrors
            .GroupBy(e => e.AgvCorrelationId)
            .Select(g => new AgvErrors(
                    g.Key,
                    _mapper.Map<List<AgvError>>(
                        g.OrderByDescending(e => e.ErrorReceived).ToList())
                )
            )
            .ToList();

        await context.RespondAsync(new GotActiveErrorsForAgvs(errors));
    }
}