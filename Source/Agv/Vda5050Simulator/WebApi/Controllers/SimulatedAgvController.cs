using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Vda5050Simulator.WebApi.Controllers;

/// <summary>
/// Controller for managing simulated agvs.
/// </summary>
[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class SimulatedAgvController(IAgvStateCache stateCache, IOptions<MqttOptions> mqttOptions, FleetHost fleet) : ControllerBase
{
    /// <summary>
    /// Switch the operating mode of an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="operatingMode">The new operating mode.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">Agv not found.</response>
    [HttpPatch]
    [Route("agv/{correlationId:guid}/switchOperatingMode")]
    public IActionResult SwitchOperatingMode([FromRoute] Guid correlationId, [FromQuery] AgvOperatingMode operatingMode)
    {
        if (!fleet.TryGetAgv(correlationId, out var id))
        {
            return NotFound();
        }

        fleet.Dispatch(new SwitchOperatingModeCommand(id, operatingMode));
        return Ok();
    }

    /// <summary>
    /// Set the agv to the given position.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="position">The new position.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">Agv not found.</response>
    /// <response code="400">Agv in automatic mode and can not be moved.</response>
    [HttpPatch]
    [Route("agv/{correlationId:guid}/move")]
    public async Task<IActionResult> SetToPosition([FromRoute] Guid correlationId, [FromBody] AgvPositionDto position, CancellationToken cancellationToken)
    {
        if (!fleet.TryGetAgv(correlationId, out var id))
        {
            return NotFound();
        }

        var result = await fleet.SendAsync(new SetPositionCommand(id, position.X, position.Y, position.Theta, position.MapId), cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Add a load to an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="load">The load to add.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">Agv not found.</response>
    /// <response code="400">The load position is already occupied.</response>
    [HttpPost]
    [Route("agv/{correlationId:guid}/load")]
    public async Task<IActionResult> AddLoad([FromRoute] Guid correlationId, [FromBody] AddLoadDto load, CancellationToken cancellationToken)
    {
        if (!fleet.TryGetAgv(correlationId, out var id))
        {
            return NotFound();
        }

        var result = await fleet.SendAsync(new AddLoadCommand(id, load.LoadId, load.LoadType, load.LoadPosition), cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Remove a load from an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <param name="loadId">The id of the load to remove.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">Agv not found.</response>
    /// <response code="400">The load does not exist.</response>
    [HttpDelete]
    [Route("agv/{correlationId:guid}/load/{loadId}")]
    public async Task<IActionResult> RemoveLoad([FromRoute] Guid correlationId, [FromRoute] string loadId, CancellationToken cancellationToken)
    {
        if (!fleet.TryGetAgv(correlationId, out var id))
        {
            return NotFound();
        }

        var result = await fleet.SendAsync(new RemoveLoadCommand(id, loadId), cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(CommandResult? result)
    {
        return result switch
        {
            null => NotFound(),
            { Success: true } => Ok(),
            _ => BadRequest(result.Error)
        };
    }

    /// <summary>
    /// Get the latest state of an agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <response code="200">The state.</response>
    /// <response code="404">No state known for the agv.</response>
    [HttpGet]
    [Route("agv/{correlationId:guid}/state")]
    public ActionResult<Vda5050StateDto> GetState([FromRoute] Guid correlationId)
    {
        if (!stateCache.TryGet(correlationId, out var id, out var state))
        {
            return NotFound();
        }

        return Ok(Vda5050StateDtoMapper.Map(id, state, mqttOptions.Value.ProtocolVersion));
    }
}
