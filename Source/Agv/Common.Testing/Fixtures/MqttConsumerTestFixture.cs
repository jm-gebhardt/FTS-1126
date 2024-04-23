using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

/// <summary>
/// Testing fixture to test MQTT consumers.
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
[PublicAPI]
public abstract class MqttConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
{
    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddMqttConsumer<TConsumer>();
    }

    protected override void ConfigureInMemory(IInMemoryBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        base.ConfigureInMemory(cfg, context);
        MqttTestFixture.ConfigureMqtt(cfg);
    }
}