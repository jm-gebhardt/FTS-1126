using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;

/// <summary>
/// Consumes AgvErrorStatusReceived message and updates AgvErrorStatus in AgvErrorRegistry
/// </summary>
[UsedImplicitly]
public class AgvErrorStatusConsumer : IConsumer<AgvErrorStatusReceived>
{
    private readonly IAgvErrorRegistry _agvErrorRegistry;

    /// <inheritdoc cref="AgvErrorStatusConsumer" />
    public AgvErrorStatusConsumer(IAgvErrorRegistry agvErrorRegistry)
    {
        _agvErrorRegistry = agvErrorRegistry;
    }

    /// <inheritdoc cref="AgvErrorStatusConsumer"/>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Consume(ConsumeContext<AgvErrorStatusReceived> context)
    {
        var message = context.Message;
        await _agvErrorRegistry.UpdateAgvErrorStatusAsync(message.Errors, message.CorrelationId);
    }
}