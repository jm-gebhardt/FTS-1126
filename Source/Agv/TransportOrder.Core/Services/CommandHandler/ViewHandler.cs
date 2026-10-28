using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Events.Views;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Views;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles view related requests and commands. While no view is stored, one generated view per level is provided.
/// </summary>
[UsedImplicitly]
public class ViewHandler(IViewRepository viewRepository, INewGraphRepository graphRepository, IBus bus, IMapper mapper, ILogger<ViewHandler> logger) :
    IConsumer<GetViews>,
    IConsumer<GetView>,
    IConsumer<GetViewBackgroundImage>,
    IConsumer<AddView>,
    IConsumer<UpdateView>,
    IConsumer<DeleteView>,
    IConsumer<SetViewBackground>,
    IConsumer<DeleteViewBackground>
{
    private static void EnsurePng(byte[] image)
    {
        using var stream = new MemoryStream(image);
        using var codec = SKCodec.Create(stream);
        if (codec?.EncodedFormat != SKEncodedImageFormat.Png)
        {
            throw new BitmapDecodingException("Only PNG images are supported.");
        }
    }

    private async Task<IReadOnlyList<View>> GetViewsAsync()
    {
        var views = await viewRepository.GetViewsAsync();
        if (views.Count > 0)
        {
            return views;
        }

        var levelAreas = await graphRepository.GetLevelAreasAsync();
        return View.CreateGenerated(levelAreas);
    }

    private async Task<View> GetViewAsync(Guid id)
    {
        var view = await viewRepository.FindViewAsync(id);
        if (view is not null)
        {
            return view;
        }

        var views = await GetViewsAsync();
        return views.SingleOrDefault(v => v.Id == id && v.IsGenerated) ?? throw new ViewNotFoundException(id);
    }

    private async Task StoreAsync(View view, CancellationToken cancellationToken, byte[]? backgroundImage = null)
    {
        await viewRepository.AddOrUpdateViewAsync(view, backgroundImage);
        await bus.Publish(new ViewChanged(view), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetViews> context)
    {
        var views = await GetViewsAsync();
        await context.RespondAsync(new GotViews(views));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetView> context)
    {
        var view = await GetViewAsync(context.Message.CorrelationId);
        await context.RespondAsync(new GotView(view));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetViewBackgroundImage> context)
    {
        var view = await GetViewAsync(context.Message.CorrelationId);
        byte[]? image = view.Background is null ? null : await viewRepository.GetBackgroundImageAsync(view.Id);

        await context.RespondAsync(new GotViewBackgroundImage(image));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AddView> context)
    {
        var view = mapper.Map<View>(context.Message);
        await StoreAsync(view, context.CancellationToken);

        logger.LogInformation("Added view {Name}/{Id}", view.Name, view.Id);
        await context.RespondAsync(new GotView(view));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<UpdateView> context)
    {
        var message = context.Message;
        var view = await GetViewAsync(message.CorrelationId);
        view.Update(message.Name, message.LevelId, message.IsDefault, message.Area);
        await StoreAsync(view, context.CancellationToken);

        logger.LogInformation("Updated view {Name}/{Id}", view.Name, view.Id);
        await context.RespondAsync(new GotView(view));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DeleteView> context)
    {
        var view = await GetViewAsync(context.Message.CorrelationId);
        if (!view.IsGenerated)
        {
            await viewRepository.DeleteViewAsync(view.Id);
            await bus.Publish(new ViewDeleted(view.Id), context.CancellationToken);
            logger.LogInformation("Deleted view {Name}/{Id}", view.Name, view.Id);
        }

        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SetViewBackground> context)
    {
        var message = context.Message;
        EnsurePng(message.Image);

        var view = await GetViewAsync(message.CorrelationId);
        view.SetBackground(mapper.Map<ViewBackground>(message));
        await StoreAsync(view, context.CancellationToken, message.Image);

        logger.LogInformation("Set background of view {Name}/{Id}", view.Name, view.Id);
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DeleteViewBackground> context)
    {
        var view = await GetViewAsync(context.Message.CorrelationId);
        if (view.Background is not null)
        {
            view.RemoveBackground();
            await StoreAsync(view, context.CancellationToken);
            logger.LogInformation("Deleted background of view {Name}/{Id}", view.Name, view.Id);
        }

        await context.RespondAsync(new Success());
    }
}