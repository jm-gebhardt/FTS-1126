using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles restricted area related requests.
/// </summary>
[UsedImplicitly]
public class RestrictedAreaRequestHandler(IBus bus, IRestrictedAreaRepository restrictedAreaRepository): 
    IConsumer<AddRestrictedArea>,
    IConsumer<TryUpdateRestrictedArea>,
    IConsumer<TryDeleteRestrictedArea>,
    IConsumer<GetRestrictedArea>,
    IConsumer<GetRestrictedAreas>
{
    /// <summary>
    /// Handles the <see cref="AddRestrictedArea"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<AddRestrictedArea> context)
    {
        var polygon = context.Message.AreaPolygon;

        if (polygon.Count < 3)
        {
            throw new InvalidPolygonLengthException();
        }

        var restrictedArea = new RestrictedArea(polygon);

        await bus.Publish(new SaveRestrictedArea(restrictedArea));

        await context.RespondAsync(new RestrictedAreaAdded(restrictedArea));
    }

    /// <summary>
    /// Handles the <see cref="TryUpdateRestrictedArea"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryUpdateRestrictedArea> context)
    {
        var (restrictedAreaId, updatedPolygon) = context.Message;

        if (updatedPolygon.Count < 3)
        {
            throw new InvalidPolygonLengthException();
        }

        var restrictedArea = await restrictedAreaRepository.GetRestrictedArea(restrictedAreaId);

        restrictedArea.UpdateArea(updatedPolygon);

        await bus.Publish(new UpdateRestrictedArea(restrictedArea));

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="TryDeleteRestrictedArea"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryDeleteRestrictedArea> context)
    {
        var restrictedArea = await restrictedAreaRepository.GetRestrictedArea(context.Message.RestrictedAreaId);

        await bus.Publish(new DeleteRestrictedArea(restrictedArea.Id));
        
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="GetRestrictedArea"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetRestrictedArea> context)
    {
        var restrictedArea = await restrictedAreaRepository.GetRestrictedArea(context.Message.RestrictedAreaId);

        await context.RespondAsync(new GotRestrictedArea(restrictedArea));
    }

    /// <summary>
    /// Handles the <see cref="GetRestrictedAreas"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetRestrictedAreas> context)
    {
        var restrictedAreas = await restrictedAreaRepository.GetRestrictedAreas(context.Message.RestrictedAreaIds.ToArray());

        await context.RespondAsync(new GotRestrictedAreas(restrictedAreas));
    }
}