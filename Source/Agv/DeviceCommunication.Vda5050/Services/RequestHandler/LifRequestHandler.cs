using FluentValidation;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Utilities;
using LifLayoutCollection = Vdma.Lif.LifLayoutCollection;
using Severity = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif.Severity;
using Gebhardt.Acc.Common.Messages.NewGraph.Requests;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;

/// <summary>
/// Handles Lif related requests.
/// </summary>
[UsedImplicitly]
public class LifRequestHandler(
    IValidator<LifLayoutCollection> lifValidator,
    IAgvRepository agvRepository,
    ILifRepository lifRepository,
    ILogger<LifRequestHandler> logger,
    IBus bus) :
    IConsumer<UploadLif>,
    IConsumer<GetLifFiles>,
    IConsumer<GetLifFile>,
    IConsumer<DeleteLifFile>,
    IConsumer<DownloadLifFile>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UploadLif> context)
    {
        (_, var uploadedTimestamp, byte[] content, string uploadedFileName, string contentType, string uploadedBy) = context.Message;

        ValidateFile(content, uploadedFileName, contentType);

        LifFile lifFile;
        string fileName = Path.GetFileName(uploadedFileName);
        double fileSizeMb = content.Length / 1024.0 / 1024.0;
        (var vdmaCollection, byte[] rawJson, ICollection<LifValidationResult> validationResults) = await ExtractFileContent(content);
        if (vdmaCollection == null)
        {
            lifFile = new LifFile { Id = Guid.NewGuid(), MetaInformation = new LifFileMetaInformation(uploadedTimestamp, fileName, fileSizeMb, uploadedBy) { ValidationResults = validationResults } };
            await context.RespondAsync(new LifValidationFailed(lifFile));
            return;
        }

        var lifLayoutCollection = vdmaCollection.ToLifLayoutCollection();
        lifFile = lifLayoutCollection.ToLifFile(uploadedTimestamp, fileName, fileSizeMb, uploadedBy);
        lifFile.RawJson = rawJson;
        lifFile.MetaInformation.ValidationResults = validationResults;

        // Check if LIF with same creator exists (workaround until update logic is implemented)
        if (await lifRepository.ExistsWithCreatorAsync(lifFile.LifCollectionMetaInformation.Creator))
        {
            validationResults.Add(new LifValidationResult(ResourceType.File, null, Severity.Err, LifValidationMessages.CreatorAlreadyExists));
            await context.RespondAsync(new LifValidationFailed(lifFile));
            return;
        }

        if (validationResults.Any(v => v.Severity == Severity.Err))
        {
            await context.RespondAsync(new LifValidationFailed(lifFile));
            return;
        }

        logger.LogInformation("LIF file '{FileName}' with {LayoutCount} layout(s) uploaded", fileName, lifFile.LayoutIds.Count);

        IRequestClient<StoreLif>? requestClient = context.CreateRequestClient<StoreLif>(bus);
        try
        {
            Response<Success, LifStoringFailed> response = await requestClient.GetResponse<Success, LifStoringFailed>(new StoreLif(lifFile, lifLayoutCollection));
            if (response.Message is Success)
            {
                await context.RespondAsync(new LifUploaded(lifFile));
                return;
            }
            if (response.Message is LifStoringFailed)
            {
                throw new LifFileUploadException("Storing file failed");
            }
        }
        catch (RequestTimeoutException)
        {
            logger.LogWarning("Storing lif file {FileName} timed out", lifFile.MetaInformation.Filename);
            throw new LifFileUploadException("Storing file timed out");
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetLifFiles> context)
    {
        ICollection<LifFile> files = await lifRepository.GetAllFilesAsync();
        await context.RespondAsync(new GotLifFiles(files));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetLifFile> context)
    {
        var file = await lifRepository.GetFileAsync(context.Message.CorrelationId);
        await context.RespondAsync(new GotLifFile(file));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DownloadLifFile> context)
    {
        var lifFile = await lifRepository.GetFileAsync(context.Message.CorrelationId);
        await context.RespondAsync(new GotLifFileDownload(lifFile));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteLifFile> context)
    {
        var id = context.Message.CorrelationId;
        var file = await lifRepository.GetFileAsync(id);
        await ValidateLifCanBeDeleted(file);

        await TriggerGraphSegmentDeletion(context, file);
        try
        {
            await lifRepository.DeleteLifAsync(id);
        }
        catch (Exception ex)
        {
            throw new LifDeletionFailedException("Failed to delete lif file", ex);
        }

        logger.LogInformation("LIF file {Filename}/{Id} deleted", file.MetaInformation.Filename, id);
        await context.RespondAsync(new Success());
    }

    private static void ValidateFile(byte[] content, string fileName, string contentType)
    {
        if (content.Length == 0)
        {
            throw new NoFileProvidedException();
        }

        if (!contentType.StartsWith(Vda5050Specifications.LifUploadFileType, StringComparison.OrdinalIgnoreCase))
        {
            throw new WrongFileTypeException(contentType, Vda5050Specifications.LifUploadFileType);
        }

        if (!fileName.EndsWith(".lif", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new WrongFileTypeException(fileName.Split(".").Last(), "json/JSON/lif/LIF");
        }
    }

    private async Task<(LifLayoutCollection? Collection, byte[] RawJson, ICollection<LifValidationResult>)> ExtractFileContent(byte[] content)
    {
        LifLayoutCollection? lifLayoutCollection;
        List<LifValidationResult> validationResults = [];

        try
        {
            using var memoryStream = new MemoryStream(content);
            using var reader = new StreamReader(memoryStream, true);
            string json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(null) }
            };
            lifLayoutCollection = JsonSerializer.Deserialize<LifLayoutCollection>(json, options);
        }
        catch (Exception ex)
        {
            validationResults.Add(new LifValidationResult(ResourceType.File, null, Severity.Err, ex.Message));
            return (null, content, validationResults);
        }

        if (lifLayoutCollection == null)
        {
            validationResults.Add(new LifValidationResult(ResourceType.File, null, Severity.Err, "No lif layout collection"));
            return (null, content, validationResults);
        }
        var validationResult = await lifValidator.ValidateAsync(lifLayoutCollection);
        if (!validationResult.IsValid)
        {
            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogError("LIF validation failed: {Errors}", errors);
            List<LifValidationResult> results = validationResult.Errors.Where(e => e.CustomState is LifValidationResult).Select(e => (LifValidationResult)e.CustomState).ToList();
            validationResults.AddRange(results);
        }

        return (lifLayoutCollection, content, validationResults);
    }

    private async Task ValidateLifCanBeDeleted(LifFile lif)
    {
        //TODO: validation need to be updated: by using LIF.MetaInformation.Creator to identify LIF files, we could have more than one LIF for a vehicleType
        ICollection<Agv> agvs = await agvRepository.GetAllForVehicleTypesAsync(lif.SupportedVehicleTypes);
        List<string> agvsRunningOrders = agvs.Where(a => a.CurrentOrder != null).Select(a => a.SerialNumber).ToList();
        if (agvsRunningOrders.Any())
        {
            throw new LifDeletionFailedException($"Failed to delete lif file: agvs {string.Join(",", agvsRunningOrders)} are currently running orders and use graph information from this lif");
        }
    }

    private async Task TriggerGraphSegmentDeletion(ConsumeContext<DeleteLifFile> context, LifFile file)
    {
        IRequestClient<DeleteGraphSegment>? requestClient = context.CreateRequestClient<DeleteGraphSegment>(bus);
        Response<Success, Failed> response = await requestClient.GetResponse<Success, Failed>(new DeleteGraphSegment(context.Message.CorrelationId, file.LifCollectionMetaInformation.Creator.ToGuid()));

        if (response.Is(out Response<Failed>? _))
        {
            throw new LifDeletionFailedException($"Could not delete graphs related to lif file {file.MetaInformation.Filename}");
        }
    }
}