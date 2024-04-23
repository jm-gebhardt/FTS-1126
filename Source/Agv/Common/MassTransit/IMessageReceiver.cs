using MassTransit;

namespace Gebhardt.Acc.Common.MassTransit;

/// <summary>
/// Receiver for a specific message.
/// </summary>
public interface IMessageReceiver<out TMessage> where TMessage : class
{
    /// <summary>
    /// Initializes the message receiver.
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the message. Only messages with this CorrelationId will be handled.</param>
    /// <param name="messageHandlerTask">The task to execute when the message is received.</param>
    /// <returns>A task which signalizes once the receiving is active.</returns>
    Task StartReceiveAsync(Guid correlationId, Func<ConsumeContext<TMessage>, Task> messageHandlerTask);
}