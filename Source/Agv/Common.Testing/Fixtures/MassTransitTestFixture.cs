using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

[PublicAPI]
public abstract class MassTransitTestFixture : FixtureBase
{
    private readonly List<Type> _consumers = new();

    /// <summary>
    /// Started TestHarness
    /// </summary>
    public ITestHarness TestHarness { get; private set; } = null!;

    protected virtual IReceivedMessageList ConsumedMessages => TestHarness.Consumed;

    protected new void Build()
    {
        AddServices(s => { s.AddMassTransitTestHarness(ConfigureMassTransitTestHarness); });

        base.Build();

        TestHarness = Provider.GetTestHarness();
        TestHarness.Start();
    }

    protected virtual void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        _consumers.ForEach(c => { busRegistrationConfigurator.AddConsumer(c); });
        busRegistrationConfigurator.AddDelayedMessageScheduler();
        busRegistrationConfigurator.UsingInMemory((context, cfg) => { ConfigureInMemory(cfg, context); });
    }

    protected virtual void ConfigureInMemory(IInMemoryBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.UseDelayedMessageScheduler();
        cfg.ConfigureEndpoints(context);
    }

    /// <summary>
    /// Adds the consumer to the service collection
    /// </summary>
    /// <returns></returns>
    protected void AddConsumers<TIConsumer>()
    {
        var item = typeof(TIConsumer);
        _consumers.Add(item);
    }

    /// <summary>
    /// Adds the list of consumer to the service collection
    /// </summary>
    /// <param name="consumers">The action of the service to add</param>
    /// <returns></returns>
    [PublicAPI]
    public MassTransitTestFixture AddConsumers(params Type[] consumers)
    {
        _consumers.AddRange(consumers);
        return this;
    }

    [PublicAPI]
    public async Task<ScheduledMessage<T>> ScheduleMessage<T>(TimeSpan delay, T message) where T : class
    {
        await using var scope = Provider.CreateAsyncScope();

        var scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
        ScheduledMessage<T> scheduledMessage = await scheduler.SchedulePublish(delay, message);
        return scheduledMessage;
    }

    /// <summary>
    /// Publish and consume the message.
    /// </summary>
    /// <param name="message">Message to be published.</param>
    /// <param name="filter">Message filter.</param>
    /// <typeparam name="T">Type of message to be published.</typeparam>
    public async Task PublishMessageAsync<T>(T message, FilterDelegate<IReceivedMessage<T>>? filter = null) where T : class, CorrelatedBy<Guid>
    {
        await PublishMessageAsync<T, Guid>(message, filter);
    }

    /// <summary>
    /// Publish and consume the message.
    /// </summary>
    /// <param name="message">Message to be published.</param>
    /// <param name="filter">Message filter.</param>
    /// <typeparam name="T">Type of message to be published.</typeparam>
    /// <typeparam name="TKey">Type of correlation id used.</typeparam>
    public async Task PublishMessageAsync<T, TKey>(T message, FilterDelegate<IReceivedMessage<T>>? filter = null)
        where T : class, CorrelatedBy<TKey>
    {
        await TestHarness.Bus.Publish(message);
        (await (filter != null ? ConsumedMessages.Any(filter) : ConsumedMessages.Any<T>(s => s.Context.Message.CorrelationId != null && s.Context.Message.CorrelationId.Equals(message.CorrelationId)))).ShouldBeTrue();
    }

    /// <summary>
    /// Get the first published message.
    /// </summary>
    /// <typeparam name="T">Type of published message.</typeparam>
    public async Task<T> GetFirstPublishedMessageAsync<T>() where T : class
    {
        await ShouldHavePublishedAsync<T>();
        var message = TestHarness.Published.Select<T>().First().Context.Message;
        return message;
    }

    /// <summary>
    /// Publish and return the message.
    /// </summary>
    /// <param name="message">Message to be published.</param>
    /// <param name="filter">Message filter.</param>
    /// <typeparam name="TPublished">Type of message to be published.</typeparam>
    /// <typeparam name="TReceived">Type of message to be received.</typeparam>
    /// <typeparam name="TKey">Type of correlation id used.</typeparam>
    public async Task<TReceived> PublishAndGetFirstMessageAsync<TPublished, TReceived, TKey>(TPublished message,
        FilterDelegate<IReceivedMessage<TPublished>>? filter = null)
        where TPublished : class, CorrelatedBy<TKey>
        where TReceived : class
    {
        await PublishMessageAsync<TPublished, TKey>(message, filter);
        return await GetFirstPublishedMessageAsync<TReceived>();
    }

    /// <summary>
    /// Publish and return the message.
    /// </summary>
    /// <param name="message">Message to be published.</param>
    /// <param name="filter">Message filter.</param>
    /// <typeparam name="TPublished">Type of message to be published.</typeparam>
    /// <typeparam name="TReceived">Type of message to be received.</typeparam>
    public async Task<TReceived> PublishAndGetFirstMessageAsync<TPublished, TReceived>(TPublished message,
        FilterDelegate<IReceivedMessage<TPublished>>? filter = null)
        where TPublished : class, CorrelatedBy<Guid>
        where TReceived : class
    {
        return await PublishAndGetFirstMessageAsync<TPublished, TReceived, Guid>(message, filter);
    }

    /// <summary>
    /// Make sure the message has been published.
    /// </summary>
    /// <typeparam name="T">Type of published message.</typeparam>
    public async Task ShouldHavePublishedAsync<T>() where T : class
    {
        (await TestHarness.Published.Any<T>()).ShouldBeTrue();
    }

    /// <summary>
    /// Make sure the message has not been published.
    /// </summary>
    /// <typeparam name="T">Type of published message.</typeparam>
    public async Task ShouldNotHavePublishedAsync<T>() where T : class
    {
        (await TestHarness.Published.Any<T>()).ShouldBeFalse();
    }
}