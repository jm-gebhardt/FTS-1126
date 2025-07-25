using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Events.RestrictedArea;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles restricted area related commands.
/// </summary>
[UsedImplicitly]
public class RestrictedAreaCommandHandler(IRestrictedAreaRepository restrictedAreaRepository, ILogger<RestrictedAreaCommandHandler> logger):
    IConsumer<SaveRestrictedArea>,
    IConsumer<UpdateRestrictedArea>,
    IConsumer<DeleteRestrictedArea>
{
    /// <summary>
    /// Handles the <see cref="SaveRestrictedArea"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SaveRestrictedArea> context)
    {
        var restrictedArea = context.Message.RestrictedArea;
        logger.LogDebug("Storing restricted area {Area}", restrictedArea.Area.Select(a => $"({a.X}, {a.Y}),"));
        await restrictedAreaRepository.AddRestrictedArea(restrictedArea);
        await context.Publish(new RestrictedAreaUpdated(restrictedArea));
    }

    /// <summary>
    /// Handles the <see cref="UpdateRestrictedArea"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateRestrictedArea> context)
    {
        var restrictedArea = context.Message.RestrictedArea;
        logger.LogDebug("Updating restricted area {Area}", restrictedArea.Area.Select(a => $"({a.X}, {a.Y}),"));
        await restrictedAreaRepository.UpdateRestrictedArea(restrictedArea);
        await context.Publish(new RestrictedAreaUpdated(restrictedArea));
    }

    /// <summary>
    /// Handles the <see cref="DeleteRestrictedArea"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<DeleteRestrictedArea> context)
    {
        var id = context.Message.RestrictedAreaId;
        logger.LogDebug("Deleting restricted area {AreaId}", id);
        await restrictedAreaRepository.DeleteRestrictedArea(id);
        await context.Publish(new RestrictedAreaDeleted(id));
    }
}