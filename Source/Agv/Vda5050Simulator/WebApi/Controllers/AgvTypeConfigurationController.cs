using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.Vda5050Simulator.WebApi.Controllers;

/// <summary>
/// Controller for managing the simulation configuration of agv types.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class AgvTypeConfigurationController(
    ILogger<AgvTypeConfigurationController> logger,
    AgvTypeConfigurationService service) : ControllerBase
{
    private static AgvTypeConfigurationDto ToDto(AgvTypeConfiguration configuration)
    {
        return new AgvTypeConfigurationDto
        {
            AgvTypeId = configuration.Id,
            AgvTypeIdentifier = configuration.AgvTypeIdentifier,
            Manufacturer = configuration.Manufacturer,
            SeriesName = configuration.SeriesName,
            SerialNumbers = configuration.SerialNumbers().ToList(),
            NoOfAgvs = configuration.NoOfAgvs,
            StateUpdateIntervalMs = configuration.StateUpdateIntervalMs,
            VisualizationUpdateIntervalMs = configuration.VisualizationUpdateIntervalMs,
            BatteryDischargeRate = configuration.BatteryDischargeRate,
            BatteryChargeRate = configuration.BatteryChargeRate,
            InitialBatteryCharge = configuration.InitialBatteryCharge,
            InitialBatteryChargeDeviation = configuration.InitialBatteryChargeDeviation,
            MaxSpeed = configuration.MaxSpeed
        };
    }

    /// <summary>
    /// Add the simulation configuration of an agv type.
    /// </summary>
    /// <param name="configuration">The configuration to add.</param>
    /// <response code="200">The stored configuration.</response>
    /// <response code="400">The agv type identifier is malformed, a value is out of range, or a configuration for that agv type already exists.</response>
    [HttpPost]
    [Route("agvTypeConfiguration")]
    public async Task<ActionResult<AgvTypeConfigurationDto>> AddAgvTypeConfiguration([FromBody] AddAgvTypeConfigurationDto configuration)
    {
        logger.LogInformation("API to add the simulation configuration of agv type {AgvTypeIdentifier} called", configuration.AgvTypeIdentifier);
        return Ok(ToDto(await service.AddAsync(configuration)));
    }

    /// <summary>
    /// Change the simulation configuration of an agv type.
    /// </summary>
    /// <param name="agvTypeId">The id of the agv type.</param>
    /// <param name="configuration">The values to change.</param>
    /// <response code="200">The stored configuration.</response>
    /// <response code="400">A value is out of range.</response>
    /// <response code="404">No configuration exists for that agv type.</response>
    [HttpPatch]
    [Route("agvTypeConfiguration/{agvTypeId:guid}")]
    public async Task<ActionResult<AgvTypeConfigurationDto>> UpdateAgvTypeConfiguration([FromRoute] Guid agvTypeId, [FromBody] UpdateAgvTypeConfigurationDto configuration)
    {
        logger.LogInformation("API to update the simulation configuration of agv type {AgvTypeId} called", agvTypeId);
        return Ok(ToDto(await service.UpdateAsync(agvTypeId, configuration)));
    }

    /// <summary>
    /// Delete the simulation configuration of an agv type and despawn its agvs.
    /// </summary>
    /// <param name="agvTypeId">The id of the agv type.</param>
    /// <response code="204">Success.</response>
    /// <response code="404">No configuration exists for that agv type.</response>
    [HttpDelete]
    [Route("agvTypeConfiguration/{agvTypeId:guid}")]
    public async Task<IActionResult> DeleteAgvTypeConfiguration([FromRoute] Guid agvTypeId)
    {
        logger.LogInformation("API to delete the simulation configuration of agv type {AgvTypeId} called", agvTypeId);
        await service.DeleteAsync(agvTypeId);
        return NoContent();
    }

    /// <summary>
    /// Get the simulation configuration of an agv type.
    /// </summary>
    /// <param name="agvTypeId">The id of the agv type.</param>
    /// <response code="200">The stored configuration.</response>
    /// <response code="404">No configuration exists for that agv type.</response>
    [HttpGet]
    [Route("agvTypeConfiguration/{agvTypeId:guid}")]
    public async Task<ActionResult<AgvTypeConfigurationDto>> GetAgvTypeConfiguration([FromRoute] Guid agvTypeId)
    {
        return Ok(ToDto(await service.GetRequiredAsync(agvTypeId)));
    }

    /// <summary>
    /// Get the simulation configurations of all agv types.
    /// </summary>
    /// <response code="200">The stored configurations, which may be empty.</response>
    [HttpGet]
    [Route("agvTypeConfigurations")]
    public async Task<ActionResult<IEnumerable<AgvTypeConfigurationDto>>> GetAgvTypeConfigurations()
    {
        return Ok((await service.GetAllAsync()).Select(ToDto));
    }
}
