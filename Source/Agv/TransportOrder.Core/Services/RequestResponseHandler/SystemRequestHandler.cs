using Gebhardt.StoreWare.Agv.Common.Time;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.System;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for system related requests.
/// </summary>
[PublicAPI]
public class SystemRequestHandler : IConsumer<GetSystemTime>
{
    private readonly ITimeProvider _timeProvider;

    /// <inheritdoc cref="SystemRequestHandler"/>
    public SystemRequestHandler(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<GetSystemTime> context)
    {
        var systemTime = _timeProvider.Now();
        var response = new GotSystemTime(systemTime);
        return context.RespondAsync(response);
    }
}