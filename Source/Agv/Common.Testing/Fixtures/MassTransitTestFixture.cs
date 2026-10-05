using Gebhardt.Acc.Common.Testing.Extensions;
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
        var scheduledMessage = await scheduler.SchedulePublish(delay, message);
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
        
        var timeout = TimeSpan.FromSeconds(10);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (await (filter != null ? ConsumedMessages.Any(filter) : ConsumedMessages.Any<T>(s => s.Context.Message.CorrelationId != null && s.Context.Message.CorrelationId.Equals(message.CorrelationId))))
            {
                return;
            }

            await Task.Delay(50);
        }
        false.ShouldBeTrue("Message was not consumed within timeout period [{timeout.TotalSeconds:0}s]");
    }

    /// <summary>
    /// Get the first published message.
    /// </summary>
    /// <param name="occurrenceNumber">
    /// The 1-based position of the matching message to return.
    /// 1 returns the first matching message, 2 returns the second, and so on.
    /// </param>
    /// <typeparam name="T">Type of published message.</typeparam>
    public Task<T> GetFirstPublishedMessageAsync<T>(int occurrenceNumber = 1) where T : class => TestHarness.Published.GetFirstPublishedMessageAsync<T>(occurrenceNumber);

    /// <summary>
    /// Publish and return the message.
    /// </summary>
    /// <param name="message">Message to be published.</param>
    /// <param name="occurrenceNumber">
    /// The 1-based position of the matching message to return.
    /// 1 returns the first matching message, 2 returns the second, and so on.
    /// </param>
    /// <param name="filter">Message filter.</param>
    /// <typeparam name="TPublished">Type of message to be published.</typeparam>
    /// <typeparam name="TReceived">Type of message to be received.</typeparam>
    /// <typeparam name="TKey">Type of correlation id used.</typeparam>
    public async Task<TReceived> PublishAndGetFirstMessageAsync<TPublished, TReceived, TKey>(TPublished message,
        int occurrenceNumber = 1,
        FilterDelegate<IReceivedMessage<TPublished>>? filter = null)
        where TPublished : class, CorrelatedBy<TKey>
        where TReceived : class
    {
        await PublishMessageAsync<TPublished, TKey>(message, filter);
        return await GetFirstPublishedMessageAsync<TReceived>(occurrenceNumber);
    }

    /// <summary>
    /// Publish and return the message.
    /// </summary>
    /// <param name="message">Message to be published.</param>
    /// <param name="occurrenceNumber">
    /// The 1-based position of the matching message to return.
    /// 1 returns the first matching message, 2 returns the second, and so on.
    /// </param>
    /// <param name="filter">Message filter.</param>
    /// <typeparam name="TPublished">Type of message to be published.</typeparam>
    /// <typeparam name="TReceived">Type of message to be received.</typeparam>
    public Task<TReceived> PublishAndGetFirstMessageAsync<TPublished, TReceived>(TPublished message,
        int occurrenceNumber = 1,
        FilterDelegate<IReceivedMessage<TPublished>>? filter = null)
        where TPublished : class, CorrelatedBy<Guid>
        where TReceived : class
    {
        return PublishAndGetFirstMessageAsync<TPublished, TReceived, Guid>(message, occurrenceNumber, filter);
    }

    /// <summary>
    /// Make sure the message has been published.
    /// </summary>
    /// <typeparam name="T">Type of published message.</typeparam>
    public Task ShouldHavePublishedAsync<T>() where T : class => TestHarness.Published.ShouldHavePublishedAsync<T>();

    /// <summary>
    /// Make sure the message has not been published.
    /// </summary>
    /// <typeparam name="T">Type of published message.</typeparam>
    public Task ShouldNotHavePublishedAsync<T>() where T : class => TestHarness.Published.ShouldNotHavePublishedAsync<T>();

    /// <summary>
    /// Get all published messages of given type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public ICollection<T> GetPublishedMessages<T>() where T : class => TestHarness.Published.GetPublishedMessages<T>();
}