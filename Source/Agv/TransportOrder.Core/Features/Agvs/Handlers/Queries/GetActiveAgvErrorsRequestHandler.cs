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
/// Defines a handler for GetAgvErrors request
/// Gets all active AgvErrors from registry by Agv correlationId returns list of AgvError
/// </summary>
[UsedImplicitly]
public class GetActiveAgvErrorsRequestHandler : IConsumer<GetActiveAgvErrorsRequest>
{
    private readonly IAgvErrorRegistry _agvErrorRegistry;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="GetAgvErrorsRequestHandler"/>
    /// <param name="agvErrorRegistry"></param>
    /// <param name="mapper"></param>
    public GetActiveAgvErrorsRequestHandler(IAgvErrorRegistry agvErrorRegistry, IMapper mapper)
    {
        _agvErrorRegistry = agvErrorRegistry;
        _mapper = mapper;
    }

    /// <inheritdoc cref="GetActiveAgvErrorsRequestHandler"/>
    /// <param name="context"></param>
    /// <returns>Returns list of active AgvErrors excluding the resolved errors</returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Consume(ConsumeContext<GetActiveAgvErrorsRequest> context)
    {
        var request = context.Message;
        IList<AgvError> agvErrors = await _agvErrorRegistry.GetActiveErrorsAsync(request.CorrelationId);
        List<AgvErrorDto> agvErrorsDto = agvErrors.Select(e => _mapper.Map<AgvErrorDto>(e)).ToList();
        await context.RespondAsync(new GetActiveAgvErrorsResult(agvErrorsDto));
    }
}