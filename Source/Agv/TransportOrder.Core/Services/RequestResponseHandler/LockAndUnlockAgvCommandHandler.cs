using JetBrains.Annotations;
using MassTransit;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the <see cref="global::TransportOrder.Messages.Commands.Agv.LockAgv"/> and <see cref="global::TransportOrder.Messages.Commands.Agv.UnlockAgv"/>
/// </summary>
[UsedImplicitly]
public class LockAndUnlockAgvCommandHandler : IConsumer<global::TransportOrder.Messages.Commands.Agv.LockAgv>, IConsumer<global::TransportOrder.Messages.Commands.Agv.UnlockAgv>
{
    private readonly IAgvRegistry _agvRegistry;
    private readonly IBus _bus;

    /// <summary>
    /// <inheritdoc cref="LockAndUnlockAgvCommandHandler"/>
    /// </summary>
    /// <param name="agvRegistry"></param>
    /// <param name="bus"></param>
    public LockAndUnlockAgvCommandHandler(IAgvRegistry agvRegistry, IBus bus)
    {
        _agvRegistry = agvRegistry;
        _bus = bus;
    }

    /// <summary>
    /// <inheritdoc cref="LockAndUnlockAgvCommandHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<global::TransportOrder.Messages.Commands.Agv.LockAgv> context)
    {
        var request = context.Message;
        var agv = _agvRegistry.GetRequiredAgv(request.CorrelationId);

        await _bus.Publish(new Common.Messages.AgvManagement.Commands.LockAgv(agv.CorrelationId));
        await context.RespondAsync(new LockAgvResult(agv.CorrelationId));
    }

    /// <summary>
    /// <inheritdoc cref="LockAndUnlockAgvCommandHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<global::TransportOrder.Messages.Commands.Agv.UnlockAgv> context)
    {
        var request = context.Message;
        var agv = _agvRegistry.GetRequiredAgv(request.CorrelationId);

        await _bus.Publish(new Common.Messages.AgvManagement.Commands.UnlockAgv(agv.CorrelationId));
        await context.RespondAsync(new UnlockAgvResult(agv.CorrelationId));
    }
}