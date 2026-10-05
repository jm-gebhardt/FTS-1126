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
public class SimulatedAgvController(
    ILogger<SimulatedAgvController> logger,
    IRequestClient<RequestOperatingModeSwitch> operatingModeClient,
    IRequestClient<RequestSetToPosition> moveRequestClient,
    IRequestClient<RequestAgvState> stateClient,
    IRequestClient<RequestAddLoad> addLoadClient,
    IRequestClient<RequestRemoveLoad> removeLoadClient,
    IMapper mapper) : Controller
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

    /// <summary>
    /// Add a load to an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="load">The load to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">Agv not found.</response>
    /// <response code="400">Agv in automatic mode or the load position is already occupied.</response>
    [HttpPost]
    [Route("agv/{correlationId:guid}/load")]
    public async Task<IActionResult> AddLoad([FromRoute] Guid correlationId, [FromBody] AddLoadDto load, CancellationToken cancellationToken)
    {
        logger.LogInformation("API to add load {LoadId} called for agv {AgvCorrelationId}", load.LoadId, correlationId);
        await addLoadClient.GetResponse<Success>(new RequestAddLoad(correlationId, load.LoadId, load.LoadType, load.LoadPosition), cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Remove a load from an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="loadId">The id of the load to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">Agv not found.</response>
    /// <response code="400">Agv in automatic mode or the load does not exist.</response>
    [HttpDelete]
    [Route("agv/{correlationId:guid}/load/{loadId}")]
    public async Task<IActionResult> RemoveLoad([FromRoute] Guid correlationId, [FromRoute] string loadId, CancellationToken cancellationToken)
    {
        logger.LogInformation("API to remove load {LoadId} called for agv {AgvCorrelationId}", loadId, correlationId);
        await removeLoadClient.GetResponse<Success>(new RequestRemoveLoad(correlationId, loadId), cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get the latest VDA5050 state of an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">The latest VDA5050 state.</response>
    /// <response code="204">The agv has not published a state yet.</response>
    /// <response code="404">Agv not found.</response>
    [HttpGet]
    [Route("agv/{correlationId:guid}/state")]
    public async Task<ActionResult<Vda5050StateDto>> GetState([FromRoute] Guid correlationId, CancellationToken cancellationToken)
    {
        var response = await stateClient.GetResponse<AgvStateResponse>(new RequestAgvState(correlationId), cancellationToken);
        if (response.Message.State == null)
        {
            return NoContent();
        }

        var dto = mapper.Map<Vda5050StateDto>(response.Message.State) with { CorrelationId = correlationId };
        return Ok(dto);
    }
}