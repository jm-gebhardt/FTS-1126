using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumes AgvErrorStatusReceived message and updates AgvErrorStatus in AgvErrorRegistry
/// </summary>
[UsedImplicitly]
public class AgvErrorStatusEventHandler : IConsumer<AgvErrorStatusReceived>
{
    private readonly IAgvErrorRegistry _agvErrorRegistry;

    /// <inheritdoc cref="AgvErrorStatusEventHandler" />
    public AgvErrorStatusEventHandler(IAgvErrorRegistry agvErrorRegistry)
    {
        _agvErrorRegistry = agvErrorRegistry;
    }

    /// <inheritdoc cref="AgvErrorStatusEventHandler"/>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Consume(ConsumeContext<AgvErrorStatusReceived> context)
    {
        var message = context.Message;
        await _agvErrorRegistry.UpdateAgvErrorStatusAsync(message.Errors, message.CorrelationId);
    }
}