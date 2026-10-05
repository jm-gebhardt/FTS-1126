using AutoMapper;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Controllers;

/// <summary>
/// Controller for managing the simulation configuration of agv types.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class SimulatedAgvTypeConfigurationController(
    ILogger<SimulatedAgvTypeConfigurationController> logger,
    IRequestClient<RequestAddAgvTypeConfiguration> addClient,
    IRequestClient<RequestUpdateAgvTypeConfiguration> updateClient,
    IRequestClient<RequestDeleteAgvTypeConfiguration> deleteClient,
    IRequestClient<RequestAgvTypeConfiguration> configurationClient,
    IRequestClient<RequestAgvTypeConfigurations> configurationsClient,
    IMapper mapper) : Controller
{
    /// <summary>
    /// Add the simulation configuration of an agv type.
    /// </summary>
    /// <param name="configuration">The configuration to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">The stored configuration.</response>
    /// <response code="400">The agv type identifier is malformed, a value is out of range, or a configuration for that agv type already exists.</response>
    [HttpPost]
    [Route("agvTypeConfiguration")]
    public async Task<ActionResult<AgvTypeConfigurationDto>> AddAgvTypeConfiguration([FromBody] AddAgvTypeConfigurationDto configuration, CancellationToken cancellationToken)
    {
        logger.LogInformation("API to add the simulation configuration of agv type {AgvTypeIdentifier} called", configuration.AgvTypeIdentifier);
        var request = new RequestAddAgvTypeConfiguration(configuration.AgvTypeIdentifier.ToGuid(), configuration.AgvTypeIdentifier, mapper.Map<AgvTypeSimulationSettings>(configuration));
        var response = await addClient.GetResponse<AgvTypeConfigurationResponse>(request, cancellationToken);
        return Ok(mapper.Map<AgvTypeConfigurationDto>(response.Message.Configuration));
    }

    /// <summary>
    /// Change the simulation configuration of an agv type.
    /// </summary>
    /// <param name="agvTypeId">The id of the agv type.</param>
    /// <param name="configuration">The values to change. The initial battery charge and its deviation only affect agvs spawned afterwards.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">The stored configuration.</response>
    /// <response code="400">A value is out of range.</response>
    /// <response code="404">No configuration exists for that agv type.</response>
    [HttpPatch]
    [Route("agvTypeConfiguration/{agvTypeId:guid}")]
    public async Task<ActionResult<AgvTypeConfigurationDto>> UpdateAgvTypeConfiguration([FromRoute] Guid agvTypeId,
        [FromBody] UpdateAgvTypeConfigurationDto configuration,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("API to update the simulation configuration of agv type {AgvTypeId} called", agvTypeId);
        var request = new RequestUpdateAgvTypeConfiguration(agvTypeId, mapper.Map<AgvTypeSimulationSettings>(configuration));
        var response = await updateClient.GetResponse<AgvTypeConfigurationResponse>(request, cancellationToken);
        return Ok(mapper.Map<AgvTypeConfigurationDto>(response.Message.Configuration));
    }

    /// <summary>
    /// Delete the simulation configuration of an agv type and despawn its agvs.
    /// </summary>
    /// <param name="agvTypeId">The id of the agv type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="204">Success.</response>
    /// <response code="404">No configuration exists for that agv type.</response>
    [HttpDelete]
    [Route("agvTypeConfiguration/{agvTypeId:guid}")]
    public async Task<IActionResult> DeleteAgvTypeConfiguration([FromRoute] Guid agvTypeId, CancellationToken cancellationToken)
    {
        logger.LogInformation("API to delete the simulation configuration of agv type {AgvTypeId} called", agvTypeId);
        await deleteClient.GetResponse<Success>(new RequestDeleteAgvTypeConfiguration(agvTypeId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get the simulation configuration of an agv type.
    /// </summary>
    /// <param name="agvTypeId">The id of the agv type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">The stored configuration.</response>
    /// <response code="404">No configuration exists for that agv type.</response>
    [HttpGet]
    [Route("agvTypeConfiguration/{agvTypeId:guid}")]
    public async Task<ActionResult<AgvTypeConfigurationDto>> GetAgvTypeConfiguration([FromRoute] Guid agvTypeId, CancellationToken cancellationToken)
    {
        var response = await configurationClient.GetResponse<AgvTypeConfigurationResponse>(new RequestAgvTypeConfiguration(agvTypeId), cancellationToken);
        return Ok(mapper.Map<AgvTypeConfigurationDto>(response.Message.Configuration));
    }

    /// <summary>
    /// Get the simulation configurations of all agv types.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">The stored configurations, which may be empty.</response>
    [HttpGet]
    [Route("agvTypeConfigurations")]
    public async Task<ActionResult<IEnumerable<AgvTypeConfigurationDto>>> GetAgvTypeConfigurations(CancellationToken cancellationToken)
    {
        var response =
            await configurationsClient.GetResponse<AgvTypeConfigurationsResponse>(new RequestAgvTypeConfigurations(Guid.NewGuid()), cancellationToken);
        return Ok(response.Message.Configurations.Select(mapper.Map<AgvTypeConfigurationDto>));
    }
}