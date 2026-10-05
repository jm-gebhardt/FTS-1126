using System.Reflection;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.InMemoryTransport.Configuration;
using MassTransit.Serialization;
using RabbitMQ.Client;

namespace Gebhardt.Acc.Common.Consumer;

/// <summary>
/// A basic MQTT consumer definition that encapsulates all necessary steps to register a consumer for receiving plain MQTT messages
/// </summary>
[PublicAPI]
public class BaseMqttConsumerDefinition<T> : ConsumerDefinition<T> where T : class, IConsumer
{
    /// <summary>
    /// Configures the consumer as mqtt consumer and uses JSON raw deserializer for the messages that are consumed.
    /// A broker endpoint accepts any message type because its routing key already selects the messages, while an
    /// in-memory endpoint shares one queue and therefore keeps filtering by message type.
    /// </summary>
    /// <param name="endpointConfigurator"></param>
    /// <param name="consumerConfigurator"></param>
    /// <param name="context"></param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<T> consumerConfigurator,
        IRegistrationContext context)
    {
        bool isInMemoryEndpoint = endpointConfigurator is IInMemoryEndpointConfiguration;

        endpointConfigurator.ConfigureConsumeTopology = isInMemoryEndpoint;
        endpointConfigurator.UseRawJsonDeserializer(isInMemoryEndpoint ? RawSerializerOptions.Default : RawSerializerOptions.AnyMessageType);
        endpointConfigurator.DefaultContentType = SystemTextJsonRawMessageSerializer.JsonContentType;

        var messages = typeof(T).GetInterfaces().Where(i => i.IsGenericType && i.Name.Contains(nameof(IConsumer))).Select(t => t.GenericTypeArguments.First());

        foreach (var message in messages)
        {
            typeof(BaseMqttConsumerDefinition<T>).GetMethod("CreateBindingByRoutingKey", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)?.MakeGenericMethod(message).Invoke(this, new object[]
            {
                endpointConfigurator
            });
        }
    }

    /// <summary>
    /// Creates a binding from the objects specified exchange (usually "amq.topic") to the consumers queue.
    /// If no routing key is specified an exception is thrown. See <see cref="RoutingKey"/>
    /// </summary>
    /// <param name="endpointConfigurator"></param>
    /// <exception cref="MissingFieldException"></exception>
    private static void CreateBindingByRoutingKey<TU>(IReceiveEndpointConfigurator endpointConfigurator) where TU : class
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rmq)
        {
            rmq.Bind<TU>(x =>
            {
                if (typeof(TU).GetCustomAttribute(typeof(RoutingKey)) is RoutingKey routingKeySpecified)
                {
                    x.RoutingKey = routingKeySpecified.RoutingKeyValue;
                }
                else
                {
                    throw new MissingFieldException("No routing key specified");
                }
                x.ExchangeType = ExchangeType.Topic;
            });
        }
    }
}