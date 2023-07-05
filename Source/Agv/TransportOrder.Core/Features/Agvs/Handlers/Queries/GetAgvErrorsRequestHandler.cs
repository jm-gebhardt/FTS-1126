using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;

/// <summary>
/// Defines a handler for GetAgvErrorHistory request
/// Gets all Agv errors from registry by Agv correlation Id returns list of AgvError
/// </summary>
[UsedImplicitly]
public class GetAgvErrorsRequestHandler : IConsumer<GetAgvErrorsRequest>
{
    private readonly IAgvErrorRegistry _agvErrorRegistry;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="GetAgvErrorsRequestHandler"/>
    public GetAgvErrorsRequestHandler(IAgvErrorRegistry agvErrorRegistry, IMapper mapper)
    {
        _agvErrorRegistry = agvErrorRegistry;
        _mapper = mapper;
    }

    /// <inheritdoc cref="GetAgvErrorsRequestHandler"/>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<GetAgvErrorsRequest> context)
    {
        IReadOnlyList<AgvError> agvErrorHistory = await _agvErrorRegistry.GetErrorsAsync(context.Message.CorrelationId, context.Message.DateFrom, context.Message.DateTo);
        List<AgvErrorDto> agvErrorHistoryDto = agvErrorHistory.Select(e => _mapper.Map<AgvErrorDto>(e)).ToList();
        await context.RespondAsync(new GetAgvErrorsResult(agvErrorHistoryDto));
    }
}