using MassTransit;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension method for <see cref="ConsumeContext"/>.
/// </summary>
public static class ConsumeContextExtension
{
    /// <summary>
    /// Publishes and responds with the given message. The response is only
    /// sent in the context of a request/response pattern.
    /// </summary>
    /// <typeparam name="TConsumedMessage">The type of the consumed message.</typeparam>
    /// <typeparam name="TMessage">The type of the message to be published/responded.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="message">The message to be published/responded.</param>
    /// <returns></returns>
    public static async Task PublishAndRespond<TConsumedMessage, TMessage>(this ConsumeContext<TConsumedMessage> context, TMessage message)
        where TMessage : class where TConsumedMessage : class
    {
        await context.Publish(message);
        if (context.ResponseAddress != null)
        {
            await context.RespondAsync(message);
        }
    }
}