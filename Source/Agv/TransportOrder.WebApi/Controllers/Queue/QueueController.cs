using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Gebhardt.Acc.Common.Domain.Constants;
using DeleteQueue = Gebhardt.Acc.TransportOrder.Messages.Requests.Queue.DeleteQueue;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.Queue;

/// <summary>
/// Queue requests
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
[Authorize]
public class QueueController : Controller
{
    private readonly IMapper _mapper;
    private readonly IRequestClient<GetQueue> _getQueueClient;
    private readonly IRequestClient<TryAddQueue> _addQueueClient;
    private readonly IRequestClient<DeleteQueue> _deleteQueueClient;
    private readonly ILogger<QueueController> _logger;
    private readonly ILoggingService _loggingService;

    /// <inheritdoc cref="QueueController"/>>
    public QueueController(IMapper mapper, ILogger<QueueController> logger, IRequestClient<GetQueue> getQueueClient, IRequestClient<DeleteQueue> deleteQueueClient, IRequestClient<TryAddQueue> addQueueClient, ILoggingService loggingService)
    {
        _mapper = mapper;
        _logger = logger;
        _deleteQueueClient = deleteQueueClient;
        _getQueueClient = getQueueClient;
        _addQueueClient=addQueueClient;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Get a queue.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Queue with given id not found.</response>
    [HttpGet]
    [Route("/queue/{id:guid}")]
    public async Task<ActionResult<QueueDto>> GetQueue([FromRoute] Guid id)
    {
        Response<GotQueue> response = await _getQueueClient.GetResponse<GotQueue>(new GetQueue(id));
        var queue = _mapper.Map<QueueDto>(response.Message.Queue);
        return Ok(queue);
    }

    /// <summary>
    /// Add a queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <param name="nodes">The queue nodes.</param>
    /// <response code="200">Ok</response>
    /// <response code="400">Queue node position or id duplicated in the nodes list.</response>
    [HttpPatch]
    [Route("/queue/")]
    public async Task<ActionResult<QueueDto>> AddQueue([FromQuery] [Required] string name, [FromBody] [Required] List<QueueNodeDto> nodes)
    {
        var queueNodes = _mapper.Map<List<QueueNode>>(nodes);
        Response<GotQueue> response = await _addQueueClient.GetResponse<GotQueue>(new TryAddQueue(name, queueNodes));
        var queue = _mapper.Map<QueueDto>(response.Message.Queue);
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemQueueAdded } });
        return Ok(queue);
    }

    /// <summary>
    /// Delete a queue.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Queue with given id not found.</response>
    [HttpDelete]
    [Route("/queue/{id:guid}")]
    public async Task<ActionResult> DeleteRestrictedArea([FromRoute] Guid id)
    {
        await _deleteQueueClient.GetResponse<Success>(new DeleteQueue(id));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemQueueDeleted } });
        return Ok();
    }
}