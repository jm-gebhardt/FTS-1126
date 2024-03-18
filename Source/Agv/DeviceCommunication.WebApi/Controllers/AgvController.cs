using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.WebApi.Dtos;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.Commands;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.WebApi.Controllers;

/// <summary>
/// Contains agv related endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
[Route("agv/{correlationId:guid}")]
public class AgvController : Controller
{
    private readonly ILogger<AgvController> _logger;
    private readonly IBus _bus;

    /// <inheritdoc cref="AgvController"/>
    public AgvController(ILogger<AgvController> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    /// <summary>
    /// Endpoint to randomly generate an error for the specified agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">No agv with this correlation id exists.</response>
    [HttpPatch]
    [Route("error")]
    public async Task<ActionResult<ErrorGeneratedDto>> GenerateError(Guid correlationId)
    {
        var error = ErrorGenerator.CreateError(correlationId);
        try
        {
            await _bus.Publish(new SimulateAgvError(correlationId, [error]));
        }
        catch (SagaException ex)
        {
            _logger.LogWarning("Could not find agv {CorrelationId} in {Handler}, {Exception}", correlationId, nameof(GenerateError), ex);
            return NotFound();
        }
        return Ok(new ErrorGeneratedDto(error));
    }
    
    /// <summary>
    /// Endpoint to randomly generate a warning for the specified agv.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    /// <response code="200">Ok</response>
    /// <response code="404">No agv with this correlation id exists.</response>
    [HttpPatch]
    [Route("warning")]
    public async Task<ActionResult<WarningGeneratedDto>> GenerateWarning(Guid correlationId)
    {
        var warning = ErrorGenerator.CreateWarning(correlationId);
        try
        {
            await _bus.Publish(new SimulateAgvError(correlationId, [warning]));
        }
        catch (SagaException ex)
        {
            _logger.LogWarning("Could not find agv {CorrelationId} in {Handler}, {Exception}", correlationId, nameof(GenerateWarning), ex);
            return NotFound();
        }
        return Ok(new WarningGeneratedDto(warning));
    }
}
