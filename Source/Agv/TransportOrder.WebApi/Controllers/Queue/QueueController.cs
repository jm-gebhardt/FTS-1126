using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
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
    private readonly IRequestClient<RemoveAgvFromQueue> _removeAgvFromQueueClient;
    private readonly IRequestClient<AdjustAgvFillQuantity> _adjustAgvFillQuantityClient;
    private readonly ILogger<QueueController> _logger;
    private readonly ILoggingService _loggingService;

    /// <inheritdoc cref="QueueController"/>>
    public QueueController(IMapper mapper,
        ILogger<QueueController> logger,
        IRequestClient<GetQueue> getQueueClient,
        IRequestClient<DeleteQueue> deleteQueueClient,
        IRequestClient<TryAddQueue> addQueueClient,
        IRequestClient<RemoveAgvFromQueue> removeAgvFromQueueClient,
        IRequestClient<AdjustAgvFillQuantity> adjustAgvFillQuantityClientClient,
        ILoggingService loggingService)
    {
        _mapper = mapper;
        _logger = logger;
        _deleteQueueClient = deleteQueueClient;
        _getQueueClient = getQueueClient;
        _addQueueClient = addQueueClient;
        _removeAgvFromQueueClient = removeAgvFromQueueClient;
        _adjustAgvFillQuantityClient = adjustAgvFillQuantityClientClient;
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
    [Authorize(Policy = AuthorizationPolicies.QueueFullAccess)]
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
    [Authorize(Policy = AuthorizationPolicies.QueueFullAccess)]
    [HttpDelete]
    [Route("/queue/{id:guid}")]
    public async Task<ActionResult> DeleteQueue([FromRoute] Guid id)
    {
        await _deleteQueueClient.GetResponse<Success>(new DeleteQueue(id));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, User.GetEmail() }, { LogLabels.Operation, LogCodes.SystemQueueDeleted } });
        return Ok();
    }

    /// <summary>
    /// Clear ReservedAgv and OccupiedAgv for a specific queue node. This will also make the other Agv move up in the queue.
    /// </summary>
    /// <param name="queueId">The id of the queue.</param>
    /// <param name="nodeId">The id of the node of the queue you want to clear.</param>
    /// <param name="body">Body.</param>
    /// <response code="200">Ok</response>
    /// <response code="400">Bad Request. E.g. AGV is not in any queue.</response>
    /// <response code="404">Queue with given id not found.</response>
    [Authorize(Policy = AuthorizationPolicies.QueueRepair)]
    [HttpPost]
    [Route("/queue/{queueId:guid}/node/{nodeId}/removeAgv")]
    public async Task<ActionResult> ClearQueueNode([FromRoute] Guid queueId, [FromRoute] string nodeId, [FromBody] RemoveAgvFromQueueNodeDto body)
    {
        string? email = User.GetEmail();
        await _removeAgvFromQueueClient.GetResponse<Success>(new RemoveAgvFromQueue(body.AgvCorrelationId, true, queueId, nodeId.ToGuid()));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, email }, { LogLabels.Operation, LogCodes.AgvRemoveFromQueue }, { "NodeId", nodeId }, { "AgvCorrelationId", body } });
        return Ok();
    }
    
    /// <summary>
    /// Adjust queue agv fill quantity. 
    /// </summary>
    /// <param name="queueId">The ID of the queue.</param>
    /// <response code="200">Ok</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">Queue with given id not found.</response>
    [Authorize(Policy = AuthorizationPolicies.QueueRepair)]
    [HttpPost]
    [Route("/queue/{queueId:guid}/adjustAgvFillQuantity")]
    public async Task<ActionResult> AdjustAgvFillQuantity ([FromRoute] Guid queueId)
    {
        string? email = User.GetEmail();
        await _adjustAgvFillQuantityClient.GetResponse<Success>(new AdjustAgvFillQuantity(queueId));
        await _loggingService.LogSystem(_logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with system", new Dictionary<string, object?> { { LogLabels.User, email }, { LogLabels.Operation, LogCodes.SystemQueueFillQuantityUpdated } });
        return Ok();
    }
}