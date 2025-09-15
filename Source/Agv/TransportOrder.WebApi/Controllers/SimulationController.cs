using System.ComponentModel.DataAnnotations;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Simulation;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TransportOrder.WebApi.Controllers;

/// <summary>
/// Provides simulation-related endpoints.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class SimulationController(
    IRequestClient<UpdateSimulationSpeed> setSimulationSpeedClient
) : Controller
{
    /// <summary>
    /// Updates the speed of the simulation.
    /// </summary>
    /// <param name="speed">The factor applied to agv velocities and acceleration to control the speed of the simulation.</param>
    /// <response code="204">No Content</response>
    [HttpPut]
    [Route("/simulation/speed")]
    public async Task<IActionResult> UpdateSimulationSpeed([FromQuery] [Required] int speed)
    {
        var command = new UpdateSimulationSpeed(speed);
        await setSimulationSpeedClient.GetResponse<Success>(command);
        return NoContent();
    }
}