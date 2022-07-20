using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.InMemoryTransport.Configuration;
using MassTransit.Serialization;
using RabbitMQ.Client;

namespace Gebhardt.StoreWare.Agv.Common.Consumer
{
    /// <summary>
    /// A basic MQTT consumer definition that encapsulates all necessary steps to register a consumer for receiving plain MQTT messages
    /// </summary>
    [PublicAPI]
    public class BaseMqttConsumerDefinition<T> : ConsumerDefinition<T> where T : class, IConsumer
    {
        /// <summary>
        /// Configures the consumer as mqtt consumer and uses JSON raw deserializer for the messages that are consumed
        /// </summary>
        /// <param name="endpointConfigurator"></param>
        /// <param name="consumerConfigurator"></param>
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<T> consumerConfigurator)
        {
            endpointConfigurator.ConfigureConsumeTopology = endpointConfigurator is IInMemoryEndpointConfiguration;

            endpointConfigurator.UseRawJsonDeserializer();
            endpointConfigurator.DefaultContentType = SystemTextJsonRawMessageSerializer.JsonContentType;

            IEnumerable<Type> messages = typeof(T).GetInterfaces().Where(i => i.IsGenericType && i.Name.Contains(nameof(IConsumer))).Select(t => t.GenericTypeArguments.First());

            foreach (var message in messages)
            {
                typeof(BaseMqttConsumerDefinition<T>).GetMethod("CreateBindingByRoutingKey", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)?.MakeGenericMethod(message).Invoke(this, new object[]{
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
}
