using System.Reflection;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Serialization;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// An extension for the MassTransit IPublishEndpoint. Tells the endpoint on the fly to send raw JSON messages
/// </summary>
[PublicAPI]
public static class RabbitMqMqttPublishExtensions
{
    /// <summary>
    /// Publish values in raw JSON format
    /// </summary>
    /// <typeparam name="T">Type of the message that shall be sent</typeparam>
    /// <param name="endpoint">The IPublishEndpoint for extension</param>
    /// <param name="values">The values to be send in <ref>T</ref></param>
    /// <param name="callback">The callback action to be invoked after sending</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Task</returns>
    public static Task PublishMqtt<T>(this IPublishEndpoint endpoint, T values, Action<PublishContext<T>>? callback = null, CancellationToken cancellationToken = default) where T : class
    {
        var routingKeySpecified = typeof(T).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;

        return endpoint.Publish(values, context =>
        {
            context.Serializer = context.Serialization.GetMessageSerializer(SystemTextJsonRawMessageSerializer.JsonContentType);
            if (routingKeySpecified != null)
            {
                context.SetRoutingKey(routingKeySpecified.RoutingKeyValue);
            }
            callback?.Invoke(context);
        }, cancellationToken);
    }

    /// <summary>
    /// Publish values in raw JSON format
    /// </summary>
    /// <typeparam name="T">Type of the message that shall be sent</typeparam>
    /// <param name="endpoint">The IPublishEndpoint for extension</param>
    /// <param name="values">The values to be send in <ref>T</ref></param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Task</returns>
    public static Task PublishMqtt<T>(this IPublishEndpoint endpoint, T values, CancellationToken cancellationToken = default) where T : class
    {
        return PublishMqtt(endpoint, values, null, cancellationToken);
    }
}