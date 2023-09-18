using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the <see cref="TransportOrder.Messages.Commands.Agv.LockAgv"/> and <see cref="TransportOrder.Messages.Commands.Agv.UnlockAgv"/>
/// </summary>
[UsedImplicitly]
public class LockAndUnlockAgvCommandHandler : IConsumer<LockAgv>, IConsumer<UnlockAgv>
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
    public async Task Consume(ConsumeContext<LockAgv> context)
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
    public async Task Consume(ConsumeContext<UnlockAgv> context)
    {
        var request = context.Message;
        var agv = _agvRegistry.GetRequiredAgv(request.CorrelationId);

        await _bus.Publish(new Common.Messages.AgvManagement.Commands.UnlockAgv(agv.CorrelationId));
        await context.RespondAsync(new UnlockAgvResult(agv.CorrelationId));
    }
}