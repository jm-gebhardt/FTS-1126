using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Trigger;

/// <summary>
/// Triggers batch requests
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
[Authorize]
public class TriggersController : Controller
{
    private readonly IRequestClient<GetTriggers> _getTriggersRequestClient;
    private readonly IRequestClient<GetDeletedTriggers> _getDeletedTriggersRequestClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="TriggersController"/>>
    public TriggersController(IRequestClient<GetTriggers> getTriggersRequestClient,
        IRequestClient<GetDeletedTriggers> getDeletedTriggersRequestClient,
        IMapper mapper)
    {
        _getTriggersRequestClient = getTriggersRequestClient;
        _getDeletedTriggersRequestClient = getDeletedTriggersRequestClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Get list of triggers.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/triggers")]
    public async Task<ActionResult<ICollection<TriggerDto>>> GetTriggers()
    {
        var response = await _getTriggersRequestClient.GetResponse<GotTriggers>(new GetTriggers());
        var triggers = _mapper.Map<ICollection<TriggerDto>>(response.Message.Triggers);
        return Ok(triggers);
    }

    /// <summary>
    /// Get list of deleted triggers.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/triggers/deleted")]
    public async Task<ActionResult<ICollection<TriggerDto>>> GetDeletedTriggers()
    {
        var response = await _getDeletedTriggersRequestClient.GetResponse<GotDeletedTriggers>(new GetDeletedTriggers());
        var triggers = _mapper.Map<ICollection<TriggerDto>>(response.Message.Triggers);
        return Ok(triggers);
    }
}