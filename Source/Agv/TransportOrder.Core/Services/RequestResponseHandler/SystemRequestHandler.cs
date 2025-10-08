using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.System;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for system related requests.
/// </summary>
[PublicAPI]
public class SystemRequestHandler :
    IConsumer<GetSystemTime>,
    IConsumer<UpdateHostOrderGeneration>
{
    private readonly TimeProvider _timeProvider;
    private readonly IBus _bus;
    private readonly ILogger _logger;

    /// <inheritdoc cref="SystemRequestHandler"/>
    public SystemRequestHandler(TimeProvider timeProvider, IBus bus, ILogger<SystemRequestHandler> logger)
    {
        _timeProvider = timeProvider;
        _bus = bus;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<GetSystemTime> context)
    {
        var systemTime = _timeProvider.GetUtcNow().UtcDateTime;
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
            _logger.LogInformation("Host order generation resumed");
        }
        else
        {
            await _bus.Publish(new PauseOrderGeneration());
            _logger.LogInformation("Host order generation paused");
        }
        await context.RespondAsync(new Success());
    }
}