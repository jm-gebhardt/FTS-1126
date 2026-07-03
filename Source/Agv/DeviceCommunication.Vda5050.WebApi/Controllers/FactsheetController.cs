using System.Text.Json;
using AutoMapper;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.FactSheet;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.FactSheet;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Dtos.FactSheet;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Controllers;

/// <summary>
/// Controller for uploading and managing fact sheets.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class FactsheetController(
    ILogger<FactsheetController> logger,
    IRequestClient<AddOrUpdateFactSheet> uploadClient,
    IMapper mapper,
    IOptions<JsonOptions> jsonOptions) : Controller
{
    /// <summary>
    /// Upload a fact sheet.
    /// </summary>
    /// <param name="factSheetJson">The fact sheet JSON.</param>
    /// <response code="204">Success.</response>
    /// <response code="400">Fact sheet not valid.</response>
    [Authorize(Policy = AuthorizationPolicies.LifFullAccess)]
    [HttpPost]
    [Route("/factsheet")]
    public async Task<IActionResult> UploadFactSheet([FromBody] JsonElement factSheetJson)
    {
        string rawJson = factSheetJson.GetRawText();
        var factSheet = factSheetJson.Deserialize<UploadFactsheetDto>(jsonOptions.Value.JsonSerializerOptions)
                        ?? throw new InvalidFactsheetException(Guid.Empty, "Empty fact sheet");

        var correlationId = factSheet.SerialNumber.ToGuid();
        logger.LogInformation("Received fact sheet for agv {AgvCorrelationId}/{SerialNumber}", correlationId, factSheet.SerialNumber);

        await uploadClient.GetResponse<Success>(new AddOrUpdateFactSheet(correlationId, mapper.Map<Factsheet>(factSheet), rawJson));

        logger.LogInformation("Processing of fact sheet for agv {AgvCorrelationId}/{SerialNumber} complete", correlationId, factSheet.SerialNumber);

        return NoContent();
    }
}