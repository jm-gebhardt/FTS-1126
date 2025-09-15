using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.System;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for system related requests.
/// </summary>
[PublicAPI]
public class SystemRequestHandler :
    IConsumer<GetSystemTime>,
    IConsumer<UpdateHostOrderGeneration>
{
    private readonly ITimeProvider _timeProvider;
    private readonly IBus _bus;

    /// <inheritdoc cref="SystemRequestHandler"/>
    public SystemRequestHandler(ITimeProvider timeProvider, IBus bus)
    {
        _timeProvider = timeProvider;
        _bus = bus;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<GetSystemTime> context)
    {
        var systemTime = _timeProvider.Now();
        var response = new GotSystemTime(systemTime);
        return context.RespondAsync(response);
    }

    /// <summary>
    /// Handles the <see cref="UpdateHostOrderGeneration"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateHostOrderGeneration> context)
    {
        if (context.Message.IsEnabled)
        {
            await _bus.Publish(new ResumeOrderGeneration());
        }
        else
        {
            await _bus.Publish(new PauseOrderGeneration());
        }
        await context.RespondAsync(new Success());
    }
}