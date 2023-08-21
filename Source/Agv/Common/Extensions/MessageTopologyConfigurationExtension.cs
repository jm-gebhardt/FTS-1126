using MassTransit;
using RabbitMQ.Client;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// A message topology extension for the RabbitMQ bus to allow raw messages to be published to a topic exchange
/// </summary>
[JetBrains.Annotations.PublicAPI]
public static class MessageTopologyConfigurationExtension
{
    /// <summary>
    /// Configure the message topology, so that the message is published as topic to an exchange
    /// </summary>
    /// <param name="configurator"></param>
    [JetBrains.Annotations.PublicAPI]
    public static void ConfigureMessageTopology<T>(this IRabbitMqBusFactoryConfigurator configurator) where T : class
    {
        configurator.Publish<T>(topologyConfigurator =>
        {
            topologyConfigurator.ExchangeType = ExchangeType.Topic;
        });
    }
}