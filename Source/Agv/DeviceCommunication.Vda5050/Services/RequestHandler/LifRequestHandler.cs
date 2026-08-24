using FluentValidation;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using LifLayoutCollection = Vdma.Lif.LifLayoutCollection;
using Severity = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif.Severity;
using ValidationException = FluentValidation.ValidationException;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;

/// <summary>
/// Handles Lif related requests.
/// </summary>
[UsedImplicitly]
public class LifRequestHandler(
    IValidator<LifLayoutCollection> lifValidator,
    IRequestClient<StoreLayouts> storeGraphLayoutsClient,
    IAgvRepository agvRepository,
    ILifRepository lifRepository,
    INurbsEdgeBuilder edgeBuilder,
    ILogger<LifRequestHandler> logger,
    IBus bus) :
    IConsumer<AddOrUpdateLif>,
    IConsumer<UploadLif>,
    IConsumer<GetLifFiles>,
    IConsumer<GetLifFile>,
    IConsumer<DeleteLifFile>,
    IConsumer<DownloadLifFile>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AddOrUpdateLif> context)
    {
        var vdmaCollection = context.Message.LifLayoutCollection;

        var validationResult = await lifValidator.ValidateAsync(vdmaCollection);
        if (!validationResult.IsValid)
        {
            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogError("LIF validation failed: {Errors}", errors);
            throw new ValidationException(validationResult.Errors);
        }
        var lifLayoutCollection = vdmaCollection.ToLifLayoutCollection();

        logger.LogInformation("Uploaded {LayoutCount} layouts, sending StoreLayouts request", lifLayoutCollection.Layouts.Length);
        await storeGraphLayoutsClient.GetResponse<LayoutStored>(new StoreLayouts(lifLayoutCollection.Layouts),
            context.CancellationToken
        );

        await context.RespondAsync(new Success());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UploadLif> context)
    {
        (var uploadedTimestamp, var file, string uploadedBy) = context.Message;

        ValidateFile(file);

        LifFile lifFile;
        string fileName = Path.GetFileName(file.FileName);
        double fileSizeMb = file.Length / 1024.0 / 1024.0;
        var (vdmaCollection, rawJson, validationResults) = await ExtractFileContent(file);
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

        await TriggerGraphDeletion(context, file);
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

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new NoFileProvidedException();
        }

        if (!file.ContentType.StartsWith(Vda5050Specifications.LifUploadFileType, StringComparison.OrdinalIgnoreCase))
        {
            throw new WrongFileTypeException(file.ContentType, Vda5050Specifications.LifUploadFileType);
        }

        if (!file.FileName.EndsWith(".lif", StringComparison.OrdinalIgnoreCase) && !file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new WrongFileTypeException(file.FileName.Split(".").Last(), "json/JSON/lif/LIF");
        }
    }

    private async Task<(LifLayoutCollection? Collection, byte[] RawJson, ICollection<LifValidationResult>)> ExtractFileContent(IFormFile file)
    {
        LifLayoutCollection? lifLayoutCollection;
        byte[] rawJson = [];
        List<LifValidationResult> validationResults = [];

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            rawJson = memoryStream.ToArray();

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            string json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true) }
            };
            lifLayoutCollection = JsonSerializer.Deserialize<LifLayoutCollection>(json, options);
        }
        catch (Exception ex)
        {
            validationResults.Add(new LifValidationResult(ResourceType.File, null, Severity.Err, ex.Message));
            return (null, rawJson, validationResults);
        }

        if (lifLayoutCollection == null)
        {
            validationResults.Add(new LifValidationResult(ResourceType.File, null, Severity.Err, "No lif layout collection"));
            return (null, rawJson, validationResults);
        }
        var validationResult = await lifValidator.ValidateAsync(lifLayoutCollection);
        if (!validationResult.IsValid)
        {
            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogError("LIF validation failed: {Errors}", errors);
            List<LifValidationResult> results = validationResult.Errors.Where(e => e.CustomState is LifValidationResult).Select(e => (LifValidationResult)e.CustomState).ToList();
            validationResults.AddRange(results);
        }
        return (lifLayoutCollection, rawJson, validationResults);
    }

    private async Task ValidateLifCanBeDeleted(LifFile lif)
    {
        ICollection<Agv> agvs = await agvRepository.GetAllForVehicleTypesAsync(lif.SupportedVehicleTypes);
        List<string> agvsRunningOrders = agvs.Where(a => a.CurrentOrder != null).Select(a => a.SerialNumber).ToList();
        if (agvsRunningOrders.Any())
        {
            throw new LifDeletionFailedException($"Failed to delete lif file: agvs {string.Join(",", agvsRunningOrders)} are currently running orders and use graph information from this lif");
        }
    }

    private async Task TriggerGraphDeletion(ConsumeContext<DeleteLifFile> context, LifFile file)
    {
        ICollection<Layout> layouts = await lifRepository.GetLayoutsByIdsAsync(file.LayoutIds);
        List<Graph> graphs = [];
        foreach (string vehicleTypeId in file.SupportedVehicleTypes)
        {
            List<Layout> layoutsForVehicle = layouts.Where(l => l.GetSupportedVehicleTypes().Contains(vehicleTypeId)).ToList();
            graphs.AddRange(layoutsForVehicle.ToGraph(vehicleTypeId, edgeBuilder));
        }
        IRequestClient<AgvTypeGraphDeleted>? requestClient = context.CreateRequestClient<AgvTypeGraphDeleted>(bus);
        Response<Success, Failed> response = await requestClient.GetResponse<Success, Failed>(new AgvTypeGraphDeleted(graphs));

        if (response.Is(out Response<Failed>? _))
        {
            throw new LifDeletionFailedException($"Could not delete graphs related to lif file {file.MetaInformation.Filename}");
        }
    }
}