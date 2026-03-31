using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Vdma.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Controllers;

/// <summary>
/// Controller for uploading and managing LIF layout collections.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class LifController(IRequestClient<AddOrUpdateLif> addOrUpdateLifClient, ILogger<LifController> logger) : Controller
{
    /// <summary>
    /// Upload a LIF layout collection.
    /// </summary>
    /// <param name="lifLayoutCollection">The new LIF layout collection.</param>
    /// <response code="204">Success.</response>
    /// <response code="400">LIF not valid.</response>
    [Authorize(Policy = AuthorizationPolicies.LifFullAccess)]
    [HttpPost]
    [Route("/lif")]
    public async Task<IActionResult> UploadLifLayoutCollection([FromBody] LifLayoutCollection lifLayoutCollection)
    {
        logger.LogInformation("Received LIF layout collection upload with {LayoutCount} layouts", lifLayoutCollection.Layouts.Length);

        await addOrUpdateLifClient.GetResponse<Success>(
            new AddOrUpdateLif(lifLayoutCollection)
        );

        logger.LogInformation("LIF processing complete.");

        return NoContent();
    }
}