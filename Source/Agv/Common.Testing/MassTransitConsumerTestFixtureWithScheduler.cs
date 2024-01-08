using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Testing fixture to test MassTransit Consumers
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
[PublicAPI]
public class MassTransitConsumerTestFixtureWithScheduler<TConsumer> where TConsumer : class, IConsumer
{
    private readonly List<Action<IServiceCollection>> _serviceActions = new();

    /// <summary>
    /// Service provider
    /// </summary>
    public ServiceProvider Provider { get; private set; } = null!;

    /// <summary>
    /// Started TestHarness
    /// </summary>
    public ITestHarness TestHarness { get; private set; } = null!;

    /// <summary>
    /// Consumer Hardness
    /// </summary>
    public IConsumerTestHarness<TConsumer> ConsumerHarness { get; private set; } = null!;

    /// <summary>
    /// Consumer test fixture to be used for consumer unit/integration tests
    /// </summary>
    /// <param name="services">Service collection instance with MassTransitTestHarness added</param>
    /// <param name="busRegistrationConfigurator">MassTransit registration with consumer already added</param>
    /// <param name="inMemoryConfiguration">MassTransit bus configurator</param>
    public MassTransitConsumerTestFixtureWithScheduler(
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        params Action<IServiceCollection>[] services)
    {
        Build(busRegistrationConfigurator, inMemoryConfiguration, services);
    }

    /// <inheritdoc cref="Build(System.Action{Microsoft.Extensions.DependencyInjection.IServiceCollection}[])"/>
    protected MassTransitConsumerTestFixtureWithScheduler<TConsumer> Build(params Action<IServiceCollection>[] services)
    {
        return Build(null, null, services);
    }

    /// <summary>
    /// Builds the service provider based on the service collection, in order to be able to register services lazy
    /// </summary>
    /// <param name="services"></param>
    /// <param name="busRegistrationConfigurator"></param>
    /// <param name="inMemoryConfiguration"></param>
    protected MassTransitConsumerTestFixtureWithScheduler<TConsumer> Build(Action<IBusRegistrationConfigurator>? busRegistrationConfigurator,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration,
        params Action<IServiceCollection>[] services)
    {
        var collection = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TConsumer>();
                x.AddDelayedMessageScheduler();
                busRegistrationConfigurator?.Invoke(x);
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();
                    cfg.ConfigureEndpoints(context);
                    inMemoryConfiguration?.Invoke(context, cfg);
                });
            });

        _serviceActions.AddRange(services);
        foreach (Action<IServiceCollection> service in _serviceActions)
        {
            service.Invoke(collection);
        }

        ConfigureServices(collection);

        Provider = collection.BuildServiceProvider(true);
        TestHarness = Provider.GetTestHarness();
        TestHarness.Start();
        ConsumerHarness = TestHarness.GetConsumerHarness<TConsumer>();

        return this;
    }

    /// <summary>
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="services">The action of the service to add</param>
    /// <returns></returns>
    public MassTransitConsumerTestFixtureWithScheduler<TConsumer> AddServices(Action<IServiceCollection> services)
    {
        _serviceActions.Add(services);
        return this;
    }

    public async Task<ScheduledMessage<T>> ScheduleMessage<T>(TimeSpan delay, T message) where T : class
    {
        await using var scope = Provider.CreateAsyncScope();

        var scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
        ScheduledMessage<T> scheduledMessage = await scheduler.SchedulePublish(delay, message);
        return scheduledMessage;
    }

    /// <summary>
    /// Is executed to configure the service collection
    /// </summary>
    /// <param name="collection"></param>
    protected virtual void ConfigureServices(IServiceCollection collection)
    {
    }

    /// <summary>
    /// Does a TestHarness.Bus.Publish and then a SagaHarness.Consumed to actually consume the message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="filter"></param>
    /// <typeparam name="T"></typeparam>
    public async Task PublishMessageAsync<T>(T message, FilterDelegate<IReceivedMessage<T>>? filter = null) where T : class, CorrelatedBy<Guid>
    {
        await TestHarness.Bus.Publish(message);
        (await (filter != null ? ConsumerHarness.Consumed.Any(filter) : ConsumerHarness.Consumed.Any<T>(s => s.Context.Message.CorrelationId == message.CorrelationId))).ShouldBeTrue();
    }

    public async Task<T> GetFirstPublishedMessageAsync<T>() where T : class
    {
        (await TestHarness.Published.Any<T>()).ShouldBeTrue();
        var message = TestHarness.Published.Select<T>().First().Context.Message;
        return message;
    }
}

/// <summary>
/// Testing fixture to test MassTransit
/// </summary>
[PublicAPI]
public class MassTransitConsumerTestFixtureWithScheduler
{
    /// <summary>
    /// Service provider
    /// </summary>
    public readonly ServiceProvider Provider;

    /// <summary>
    /// Started TestHarness
    /// </summary>
    public readonly ITestHarness TestHarness;

    /// <summary>
    /// Consumer Test Fixture to be used for Consumer Unit/Integration tests
    /// </summary>
    /// <param name="services">Service Collection instance with MassTransitTestHarness added</param>
    /// <param name="busRegistrationConfigurator">MassTransit Registration with consumer already added</param>
    /// <param name="inMemoryConfiguration">MassTransit bus configurator</param>
    public MassTransitConsumerTestFixtureWithScheduler(
        Action<IServiceCollection>? services = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null)
    {
        var collection = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                busRegistrationConfigurator?.Invoke(x);
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                    inMemoryConfiguration?.Invoke(context, cfg);
                });
            });

        services?.Invoke(collection);

        Provider = collection.BuildServiceProvider(true);
        TestHarness = Provider.GetTestHarness();
        TestHarness.Start();
    }
}