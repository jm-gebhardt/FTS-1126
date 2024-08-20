using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.KaeserSidecar.Configuration;

/// <inheritdoc/>
[UsedImplicitly]
public class SingleActiveHandlerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
where TConsumer: class, IConsumer
{
    /// <inheritdoc/>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator, IRegistrationContext context)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbitMqConfigurator)
        {
            rabbitMqConfigurator.SingleActiveConsumer = true;
        }
    }
}

