using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Vda5050Simulator.Configuration;
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
