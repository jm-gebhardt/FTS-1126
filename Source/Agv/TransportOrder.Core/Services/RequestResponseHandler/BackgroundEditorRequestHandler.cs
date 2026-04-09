using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles background (editor) related requests
/// </summary>
[UsedImplicitly]
public class BackgroundEditorRequestHandler(IBackgroundEditorRepository backgroundEditorRepository, ILogger<BackgroundEditorRequestHandler> logger, ILoggingService loggingService) :
    IConsumer<AddOrUpdateBackground>,
    IConsumer<GetBackground>,
    IConsumer<DeleteBackgroundScene>,
    IConsumer<GetBackgrounds>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AddOrUpdateBackground> context)
    {
        var background = context.Message.Background;
        logger.LogInformation("Adding background {Name}/{Id} for map {MapId}", background.Name, background.Id, background.MapId);

        await backgroundEditorRepository.AddOrUpdateBackgroundAsync(background);
        await loggingService.LogSystem(logger, LogCodes.SystemBackgroundAddedOrUpdated, "Background updated", "Background added or updated", new Dictionary<string, object?>() { { LogLabels.BackgroundName, background.Name } });
        await context.RespondAsync(new BackgroundAddedOrUpdated(background));
    }

    /// <inheritdoc/>
    public async  Task Consume(ConsumeContext<GetBackground> context)
    {
        var id = context.Message.Id;

        logger.LogDebug("Getting background {Id}", id);
        var background = await backgroundEditorRepository.GetRequiredBackgroundAsync(id);

        await context.RespondAsync(new GotBackground(background));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DeleteBackgroundScene> context)
    {
        var id = context.Message.Id;

        logger.LogInformation("Deleting background {Id}", id);
        var background = await backgroundEditorRepository.GetRequiredBackgroundAsync(id);
        await backgroundEditorRepository.DeleteBackgroundAsync(id);
        await loggingService.LogSystem(logger, LogCodes.SystemBackgroundDeleted, "Background deleted", "Background deleted", new Dictionary<string, object?>() { { LogLabels.BackgroundName, background.Name } });
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetBackgrounds> context)
    {
        (ICollection<Guid> mapIds, int maxCount) = context.Message;

        ICollection<Background> backgrounds = await backgroundEditorRepository.GetBackgroundsAsync(mapIds, maxCount);
        await context.RespondAsync(new GotBackgrounds(backgrounds));
    }
}