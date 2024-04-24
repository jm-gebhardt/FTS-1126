using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

/// <summary>
/// Testing fixture to test MassTransit Consumers
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
[PublicAPI]
public abstract class MassTransitConsumerTestFixture<TConsumer> : MassTransitTestFixture where TConsumer : class, IConsumer
{
    /// <summary>
    /// Consumer Harness
    /// </summary>
    public IConsumerTestHarness<TConsumer> ConsumerHarness { get; private set; } = null!;

    protected override IReceivedMessageList ConsumedMessages => ConsumerHarness.Consumed;

    /// <summary>
    /// Consumer test fixture to be used for consumer unit/integration tests
    /// </summary>
    protected MassTransitConsumerTestFixture()
    {
        AddServices(s => s.AddSingleton(new Mock<ILogger<TConsumer>>().Object));
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddConsumer<TConsumer>();
    }

    protected new MassTransitConsumerTestFixture<TConsumer> Build()
    {
        base.Build();

        ConsumerHarness = TestHarness.GetConsumerHarness<TConsumer>();
        return this;
    }
    
    /// <summary>
    /// Get the first consumed message
    /// </summary>
    /// <typeparam name="T">Type of consumed message.</typeparam>
    public async Task<T> GetFirstConsumedMessageAsync<T>() where T : class
    {
        await ShouldHavePublishedAsync<T>();
        var message = ConsumerHarness.Consumed.Select<T>().First().Context.Message;
        return message;
    }
    
    /// <summary>
    /// Check if the message has been consumed
    /// </summary>
    /// <typeparam name="T">Type of consumed message.</typeparam>
    public async Task ShouldHaveConsumedAsync<T>() where T : class
    { 
        (await ConsumerHarness.Consumed.Any<T>()).ShouldBeTrue();
    }
    
    /// <summary>
    /// Check if the message has not been consumed
    /// </summary>
    /// <typeparam name="T">Type of consumed message.</typeparam>
    public async Task ShouldNotHaveConsumedAsync<T>() where T : class
    { 
        (await ConsumerHarness.Consumed.Any<T>()).ShouldBeFalse();
    }
}