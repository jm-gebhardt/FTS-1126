using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;
using Gebhardt.Acc.TransportOrder.Messages.Requests.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;
using Gebhardt.Acc.TransportOrder.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers.AgvType;

/// <summary>
/// AgvType related requests.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class AgvTypeController(ILogger<AgvTypeController> logger, ILoggingService loggingService,
                               IRequestClient<RequestAgvTypeAcknowledgement> acknowledgementClient,
                               IRequestClient<GetAgvType> getOneClient,
                               IRequestClient<GetAllAgvTypes> getAllClient,
                               IMapper mapper) : Controller
{
    /// <summary>
    /// Acknowledge an agv type.
    /// </summary>
    /// <param name="id">The id of the agv type.</param>
    /// <param name="cancellationToken">Token cancelled if the request is aborted.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv type with given id not found.</response>
    [HttpPatch]
    [Authorize(Policy = AuthorizationPolicies.AgvTypeFull)]
    [Route("/agvType/{id:guid}/acknowledge")]
    public async Task<ActionResult> AcknowledgeAgvType([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        await loggingService.LogSystem(logger, LogCodes.SystemOperatorInteraction, "Operator interaction", "Operator interacted with agv type", new Dictionary<string, object?> { { LogLabels.Operation, LogCodes.SystemAgvTypeAcknowledged }, { LogLabels.User, email } });
        await acknowledgementClient.GetResponse<Success>(new RequestAgvTypeAcknowledgement(id), cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get an agv type.
    /// </summary>
    /// <param name="id">The id of the agv type.</param>
    /// <param name="cancellationToken">Token cancelled if the request is aborted.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv type with given id not found.</response>
    [HttpGet]
    [Route("/agvType/{id:guid}")]
    public async Task<ActionResult<AgvTypeDto>> GetAgvType([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await getOneClient.GetResponse<GotAgvType>(new GetAgvType(id), cancellationToken);
        var agvType = mapper.Map<AgvTypeDto>(response.Message.AgvType);
        return Ok(agvType);
    }

    /// <summary>
    /// Get all agv types.
    /// </summary>
    /// <param name="cancellationToken">Token cancelled if the request is aborted.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">Agv type with given id not found.</response>
    [HttpGet]
    [Route("/agvTypes")]
    public async Task<ActionResult<ICollection<AgvTypeDto>>> GetAllAgvTypes(CancellationToken cancellationToken)
    {
        var response = await getAllClient.GetResponse<GotAgvTypes>(new GetAllAgvTypes(Guid.NewGuid()), cancellationToken);
        var agvTypes = mapper.Map<ICollection<AgvTypeDto>>(response.Message.AgvTypes);
        return Ok(agvTypes);
    }
}