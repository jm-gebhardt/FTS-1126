namespace Gebhardt.StoreWare.Agv.Common.MassTransit;

/// <summary>
/// Service to wait for a specific message with a specific condition to be received. Additionally it has a timeout.
/// </summary>
public interface IMessageAwaiter<TMessage> where TMessage : class
{
    /// <summary>
    /// Waits for a specific message to be received and returns once the right message has been received.
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the message. Only messages with this CorrelationId will be handled.</param>
    /// <param name="successFunction">Is evaluated in order to find out if the right message has been received.</param>
    /// <param name="cancellationToken">To cancel the receiving.</param>
    /// <param name="executeAfterListeningTask">This Task is executed once the listening is active, to e.g. send a message out.</param>
    /// <param name="checkContinuouslyUntilSuccess">If false the first wrong message cancels the receiving.</param>
    /// <returns></returns>
    Task<TMessage> WaitAsync(Guid correlationId, Func<TMessage, bool> successFunction,
        CancellationToken cancellationToken,
        Func<Task>? executeAfterListeningTask = null,
        bool checkContinuouslyUntilSuccess = false);
}