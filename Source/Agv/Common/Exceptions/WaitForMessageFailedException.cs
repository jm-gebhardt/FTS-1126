namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Thrown when waiting for a message failed.
/// </summary>
public class WaitForMessageFailedException<TMessage> : Exception
{
    /// <summary>
    /// The received message, if any.
    /// </summary>
    public TMessage? ReceivedMessage { get; }

    /// <inheritdoc cref="WaitForMessageFailedException{TMessage}"/>
    /// <param name="reason"></param>
    /// <param name="receivedMessage"></param>
    public WaitForMessageFailedException(string reason, TMessage? receivedMessage = default) : base($"{reason} {typeof(TMessage).Name}")
    {
        ReceivedMessage = receivedMessage;
    }
}