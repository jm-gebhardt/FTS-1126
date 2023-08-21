using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;

/// <summary>
/// Defines a handler for the GetAgvActionGroups request
/// </summary>
[PublicAPI]
public class GetAgvActionGroupsRequestHandler : IConsumer<GetAgvActionGroupsRequest>
{
    private readonly IActionGroupRetrievalService _retrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    ///  Constructor
    /// </summary>
    /// <param name="retrievalService"></param>
    /// <param name="mapper"></param>
    public GetAgvActionGroupsRequestHandler(IActionGroupRetrievalService retrievalService, IMapper mapper)
    {
        _retrievalService = retrievalService;
        _mapper = mapper;
    }

    /// <summary>
    /// <inheritdoc cref="GetAgvActionGroupsRequestHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Consume(ConsumeContext<GetAgvActionGroupsRequest> context)
    {
        var request = context.Message;
        IReadOnlyList<ActionGroupReadModel> actionGroups = await _retrievalService.GetActionGroupsWithTargetNodeTypeAsync(request.States, request.FromDate, request.ToDate, request.Limit);

        var result = new GetAgvActionGroupsResult(actionGroups.Select(g => _mapper.Map<ActionGroupDto>(g)).ToList());

        await context.RespondAsync(result);
    }
}