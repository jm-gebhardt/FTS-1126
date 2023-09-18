using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides a command handler for the <see cref="SetAgvForceIdle"/> command.
/// </summary>
public class SetAgvForceIdleCommandHandler: IConsumer<SetAgvForceIdle>
{

    private readonly IBus _bus;
    private readonly IAgvRegistry _agvRegistry;
    private readonly ILogger<SetAgvForceIdleCommandHandler> _logger;

    /// <summary>
    /// Initializes a <see cref="SetAgvForceIdleCommandHandler"/> instance.
    /// </summary>
    [UsedImplicitly]
    public SetAgvForceIdleCommandHandler(IBus bus, IAgvRegistry agvRegistry,ILogger<SetAgvForceIdleCommandHandler> logger)
    {
        _bus = bus;
        _agvRegistry = agvRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Provides a command handler for the <see cref="SetAgvForceIdle"/> command.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<SetAgvForceIdle> context)
    {
        var correlationId = context.Message.CorrelationId;

        bool agvExists = await _agvRegistry.CheckIfAgvExistsAsync(correlationId);
        if (!agvExists)
        {
            _logger.LogError("Agv {correlationId} not found for {command}.", correlationId, nameof(SetAgvForceIdle));
            throw new AgvNotFoundException(correlationId);
        }

        await _bus.Publish(new SetAgvForceIdleRequested(correlationId));
        await context.RespondAsync(new SetAgvForceIdleResult(correlationId));
    }
}