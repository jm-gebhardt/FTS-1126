using Gebhardt.Acc.Common.Exceptions;

namespace Gebhardt.Acc.Common.MassTransit;

/// <inheritdoc />
public class MessageAwaiter<TMessage> : IMessageAwaiter<TMessage> where TMessage : class
{
    private readonly IMessageReceiver<TMessage> _messageReceiver;

    /// <inheritdoc cref="IMessageAwaiter{TMessage}"/>
    /// <param name="messageReceiver"></param>
    public MessageAwaiter(IMessageReceiver<TMessage> messageReceiver)
    {
        _messageReceiver = messageReceiver;
    }

    /// <inheritdoc />
    public async Task<TMessage> WaitAsync(Guid correlationId, Func<TMessage, bool> successFunction,
        CancellationToken cancellationToken,
        Func<Task>? executeAfterListeningTask = null,
        bool checkContinuouslyUntilSuccess = false)
    {
        var completionSource = new TaskCompletionSource<(bool Success, TMessage Message)>();

        await _messageReceiver.StartReceiveAsync(correlationId,
            context =>
            {
                if (successFunction(context.Message))
                {
                    completionSource.TrySetResult((true, context.Message));
                }
                else if (!checkContinuouslyUntilSuccess)
                {
                    completionSource.TrySetResult((false, context.Message));
                }
                return Task.CompletedTask;
            });

        if (executeAfterListeningTask != null)
        {
            await executeAfterListeningTask();
        }

        if (await Task.WhenAny(completionSource.Task, Task.Delay(Timeout.Infinite, cancellationToken)) != completionSource.Task)
        {
            throw new WaitForMessageFailedException<TMessage>("Timed out without receiving message.");
        }

        var messageResult = completionSource.Task.Result;
        if (messageResult.Success)
        {
            return messageResult.Message;
        }

        throw new WaitForMessageFailedException<TMessage>("Message received, but was not evaluated as success.", messageResult.Message);
    }
}