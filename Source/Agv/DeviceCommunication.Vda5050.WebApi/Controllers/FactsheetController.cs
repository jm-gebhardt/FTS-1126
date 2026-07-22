using System.Text.Json;
using AutoMapper;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Dtos.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Controllers;

/// <summary>
/// Controller for uploading and managing factsheets.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class FactsheetController(
    ILogger<FactsheetController> logger,
    IRequestClient<AddOrUpdateFactsheet> uploadClient,
    IMapper mapper,
    IOptions<JsonOptions> jsonOptions) : Controller
{
    /// <summary>
    /// Upload a factsheet.
    /// </summary>
    /// <param name="factsheetJson">The factsheet JSON.</param>
    /// <response code="204">Success.</response>
    /// <response code="400">Factsheet not valid.</response>
    [Authorize(Policy = AuthorizationPolicies.LifFullAccess)]
    [HttpPost]
    [Route("/factsheet")]
    public async Task<IActionResult> UploadFactsheet([FromBody] JsonElement factsheetJson)
    {
        string rawJson = factsheetJson.GetRawText();
        var factsheet = factsheetJson.Deserialize<UploadFactsheetDto>(jsonOptions.Value.JsonSerializerOptions)
                        ?? throw new InvalidFactsheetException(Guid.Empty, "Empty factsheet");

        var correlationId = factsheet.SerialNumber.ToGuid();
        logger.LogInformation("Received factsheet for agv {AgvCorrelationId}/{SerialNumber}", correlationId, factsheet.SerialNumber);

        await uploadClient.GetResponse<Success>(new AddOrUpdateFactsheet(correlationId, mapper.Map<Factsheet>(factsheet), rawJson));

        logger.LogInformation("Processing of factsheet for agv {AgvCorrelationId}/{SerialNumber} complete", correlationId, factsheet.SerialNumber);

        return NoContent();
    }
}