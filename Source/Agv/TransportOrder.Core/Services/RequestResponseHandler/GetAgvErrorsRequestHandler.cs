using AutoMapper;
using JetBrains.Annotations;
using MassTransit;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Requests.Agv;
using AgvError = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvError;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for GetAgvErrorHistory request
/// Gets all Agv errors from registry by Agv correlation Id returns list of AgvError
/// </summary>
[UsedImplicitly]
public class GetAgvErrorsRequestHandler : IConsumer<GetAgvErrorsRequest>,
                                          IConsumer<GetActiveAgvErrorsRequest>
{
    private readonly IAgvErrorRegistry _agvErrorRegistry;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="GetAgvErrorsRequestHandler"/>
    public GetAgvErrorsRequestHandler(IAgvErrorRegistry agvErrorRegistry, IMapper mapper)
    {
        _agvErrorRegistry = agvErrorRegistry;
        _mapper = mapper;
    }

    /// <summary>
    /// Handle the <see cref="GetAgvErrorsRequest"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetAgvErrorsRequest> context)
    {
        IReadOnlyList<AgvError> agvErrorHistory = await _agvErrorRegistry.GetErrorsAsync(context.Message.CorrelationId, context.Message.DateFrom, context.Message.DateTo);
        List<global::TransportOrder.Messages.Requests.Agv.AgvError> agvErrorHistoryMessageContent = agvErrorHistory.Select(e => _mapper.Map<global::TransportOrder.Messages.Requests.Agv.AgvError>(e)).ToList();
        await context.RespondAsync(new GetAgvErrorsResult(agvErrorHistoryMessageContent));
    }

    /// <summary>
    /// Handle the <see cref="GetActiveAgvErrorsRequest"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetActiveAgvErrorsRequest> context)
    {
        var request = context.Message;
        IList<AgvError> agvErrors = await _agvErrorRegistry.GetActiveErrorsAsync(request.CorrelationId);
        List<global::TransportOrder.Messages.Requests.Agv.AgvError> agvErrorsMessageContent = agvErrors.Select(e => _mapper.Map<global::TransportOrder.Messages.Requests.Agv.AgvError>(e)).ToList();
        await context.RespondAsync(new GetActiveAgvErrorsResult(agvErrorsMessageContent));
    }
}