using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Dtos.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Policies;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Controllers;

/// <summary>
/// Controller for uploading and managing LIF layout collections.
/// </summary>
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = RestApiGroup.GroupName)]
public class LifController(
    IRequestClient<UploadLif> uploadLifClient,
    IRequestClient<GetLifFiles> getLifsClient,
    IRequestClient<GetLifFile> getLifClient,
    IRequestClient<DeleteLifFile> deleteLifClient,
    IRequestClient<DownloadLifFile> downloadLifClient,
    TimeProvider timeProvider,
    ILogger<LifController> logger,
    IMapper mapper) : Controller
{
    /// <summary>
    /// Upload a LIF file.
    /// </summary>
    /// <param name="file">The LIF file.</param>
    /// <response code="200">Success.</response>
    /// <response code="400">LIF not valid.</response>
    [Authorize(Policy = AuthorizationPolicies.LifFullAccess)]
    [HttpPost]
    [Route("/lif/file")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FileMetaInformationDto>> UploadLifFile(IFormFile file)
    {
        string? email = User.GetEmail();
        if (email == null)
        {
            return Unauthorized();
        }
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        byte[] content = memoryStream.ToArray();

        var request = new UploadLif(Guid.NewGuid(), timeProvider.GetUtcNow().UtcDateTime, content, file.FileName, file.ContentType, email);
        var response = await uploadLifClient.GetResponse<LifUploaded, LifValidationFailed>(request);
        FileMetaInformationDto? lifFile;
        if (response.Message is LifUploaded uploaded)
        {
            lifFile = mapper.Map<FileMetaInformationDto>(uploaded.LifFile);
            logger.LogInformation("LIF file upload complete.");
            return Ok(lifFile);
        }
        if (response.Message is LifValidationFailed wrongUpload)
        {
            lifFile = mapper.Map<FileMetaInformationDto>(wrongUpload.LifFile);
            logger.LogWarning("LIF file upload failed.");
            return BadRequest(lifFile);
        }
        return Conflict();
    }

    /// <summary>
    /// Get all LIF file meta data.
    /// </summary>
    /// <response code="200">Success.</response>
    [HttpGet]
    [Route("/lif/files")]
    public async Task<ActionResult<ICollection<FileMetaInformationDto>>> GetAllLifs()
    {
        var response = await getLifsClient.GetResponse<GotLifFiles>(new GetLifFiles());
        var lifFiles = mapper.Map<List<FileMetaInformationDto>>(response.Message.LifFiles);
        return Ok(lifFiles);
    }

    /// <summary>
    /// Delete a lif file
    /// </summary>
    /// <response code="200">Success.</response>
    /// <response code="404">Lif file not found.</response>
    [HttpDelete]
    [Route("/lif/file/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.LifFullAccess)]
    public async Task<ActionResult> DeleteLifFile([FromRoute] Guid id)
    {
        await deleteLifClient.GetResponse<Success>(new DeleteLifFile(id));
        return Ok();
    }

    /// <summary>
    /// Get file meta data for a single lif file.
    /// </summary>
    /// <response code="200">Success.</response>
    /// <response code="404">Lif file not found.</response>
    [HttpGet]
    [Route("/lif/file/{id:guid}")]
    public async Task<ActionResult<FileMetaInformationDto>> GetLifFile([FromRoute] Guid id)
    {
        var response = await getLifClient.GetResponse<GotLifFile>(new GetLifFile(id));
        var lifFile = mapper.Map<FileMetaInformationDto>(response.Message.LifFile);
        return Ok(lifFile);
    }

    /// <summary>
    /// Download a lif file as JSON.
    /// </summary>
    /// <response code="200">Success.</response>
    /// <response code="404">Lif file not found.</response>
    [HttpGet]
    [Route("/lif/file/{id:guid}/download")]
    [Produces("application/json")]
    public async Task<IActionResult> DownloadLifFile([FromRoute] Guid id)
    {
        var response = await downloadLifClient.GetResponse<GotLifFileDownload>(new DownloadLifFile(id));
        var lifFile = response.Message.LifFile;
        logger.LogInformation("Downloading LIF file {Filename}", lifFile.MetaInformation.Filename);
        return File(lifFile.RawJson!, "application/json", lifFile.MetaInformation.Filename);
    }
}