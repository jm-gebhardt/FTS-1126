using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Queue;

/// <summary>
/// Queues requests
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
[Authorize]
public class QueuesController : Controller
{
    private readonly IMapper _mapper;
    private readonly IRequestClient<GetQueues> _getQueuesClient;

    /// <inheritdoc cref="QueuesController"/>>
    public QueuesController(IMapper mapper, IRequestClient<GetQueues> getQueuesClient)
    {
        _mapper=mapper;
        _getQueuesClient=getQueuesClient;
    }

    /// <summary>
    /// Get all queues.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/queues")]
    public async Task<ActionResult<ICollection<QueueDto>>> GetQueues()
    {
        Response<GotQueues> response = await _getQueuesClient.GetResponse<GotQueues>(new GetQueues());
        var queues = _mapper.Map<List<QueueDto>>(response.Message.Queues);
        return Ok(queues);
    }
}