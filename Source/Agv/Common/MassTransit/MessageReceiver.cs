using MassTransit;

namespace Gebhardt.Acc.Common.MassTransit;

/// <inheritdoc cref="IMessageReceiver{T}"/>
public class MessageReceiver<TMessage> : IMessageReceiver<TMessage>, IAsyncDisposable where TMessage : class
{
    private readonly IBus _bus;
    private HostReceiveEndpointHandle? _handle;

    /// <inheritdoc cref="MessageReceiver{TMessage}"/>
    /// <param name="bus"></param>
    public MessageReceiver(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    public Task StartReceiveAsync(Guid correlationId, Func<ConsumeContext<TMessage>, Task> messageHandlerTask)
    {
        _handle = _bus.ConnectReceiveEndpoint(ep =>
        {
            ep.Handler<TMessage>(async context =>
            {
                if (context.CorrelationId == correlationId)
                {
                    await messageHandlerTask(context);
                }
            });
        });

        return _handle.Ready;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_handle != null)
        {
            await _handle.StopAsync();
        }

        GC.SuppressFinalize(this);
    }
}