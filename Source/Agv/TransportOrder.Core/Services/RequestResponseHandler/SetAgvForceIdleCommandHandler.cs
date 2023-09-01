using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Commands.Agv;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;

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