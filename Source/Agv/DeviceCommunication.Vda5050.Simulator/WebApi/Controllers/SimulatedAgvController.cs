using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AgvPosition = Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.AgvPosition;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Controllers;

/// <summary>
/// Controller for managing simulated agvs.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class SimulatedAgvController(ILogger<SimulatedAgvController> logger, IRequestClient<RequestOperatingModeSwitch> operatingModeClient, IRequestClient<RequestSetToPosition> moveRequestClient, IMapper mapper) : Controller
{
    /// <summary>
    /// Switch the operating mode of an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="operatingMode">The new operating mode.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">Agv not found.</response>
    [HttpPatch]
    [Route("agv/{correlationId:guid}/switchOperatingMode")]
    public async Task<IActionResult> SwitchOperatingMode([FromRoute] Guid correlationId, [FromQuery] AgvOperatingMode operatingMode, CancellationToken cancellationToken)
    {
        logger.LogInformation("API to switch operation mode called for agv {AgvCorrelationId}", correlationId);
        await operatingModeClient.GetResponse<Success>(new RequestOperatingModeSwitch(correlationId, operatingMode), cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Set the agv to the given position.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="position">The new position.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">Agv not found.</response>
    /// <response code="400">Agv in automatic mode and can not be moved.</response>
    [HttpPatch]
    [Route("agv/{correlationId:guid}/move")]
    public async Task<IActionResult> SetToPosition([FromRoute] Guid correlationId, [FromBody] AgvPositionDto position, CancellationToken cancellationToken)
    {
        await moveRequestClient.GetResponse<Success>(new RequestSetToPosition(correlationId, mapper.Map<AgvPosition>(position)), cancellationToken);
        return Ok();
    }
}