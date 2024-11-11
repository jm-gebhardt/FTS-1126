using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles factory time related requests.
/// </summary>
[UsedImplicitly]
public class FactoryTimeRequestHandler(IFactoryTimeRepository factoryTimeRepository) : 
    IConsumer<UpdateFactoryTimeZone>,
    IConsumer<GetFactoryTimeZone>
{
    /// <summary>
    /// Handles the <see cref="UpdateFactoryTimeZone"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateFactoryTimeZone> context)
    {
        var factoryTime = context.Message.TimeZone;
        await factoryTimeRepository.SetFactoryTime(factoryTime);
        await context.RespondAsync(new UpdatedFactoryTimeZone(true));
    }

    /// <summary>
    /// Handles the <see cref="GetFactoryTimeZone"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetFactoryTimeZone> context)
    {
        var factoryTime = await factoryTimeRepository.GetFactoryTime();
        await context.RespondAsync(new GotFactoryTimeZone(factoryTime.FactoryTimeZone, factoryTime.Dirty));
    }
}