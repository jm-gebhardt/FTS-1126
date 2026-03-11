using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;
using Shouldly;

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

    /// <summary>
    /// Publish and consume the mqtt message.
    /// </summary>
    /// <param name="message">Message to be published.</param>
    /// <typeparam name="T">Type of message to be published.</typeparam>
    public async Task PublishMqttMessageAsync<T>(T message)
        where T : class
    {
        await TestHarness.Bus.Publish(message, context => context.SetRoutingKey("routing.key"));
        ConsumedMessages.Select<T>().Count().ShouldBe(1);
    }
}