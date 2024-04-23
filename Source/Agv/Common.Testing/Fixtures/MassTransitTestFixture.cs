using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

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

    public async Task<ScheduledMessage<T>> ScheduleMessage<T>(TimeSpan delay, T message) where T : class
    {
        await using var scope = Provider.CreateAsyncScope();

        var scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
        ScheduledMessage<T> scheduledMessage = await scheduler.SchedulePublish(delay, message);
        return scheduledMessage;
    }

    public async Task<T> GetFirstPublishedMessageAsync<T>() where T : class
    {
        (await TestHarness.Published.Any<T>()).ShouldBeTrue();
        var message = TestHarness.Published.Select<T>().First().Context.Message;
        return message;
    }

    /// <summary>
    /// Publish and consume the message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="filter"></param>
    /// <typeparam name="T"></typeparam>
    public async Task PublishMessageAsync<T>(T message, FilterDelegate<IReceivedMessage<T>>? filter = null) where T : class, CorrelatedBy<Guid>
    {
        await TestHarness.Bus.Publish(message);
        (await (filter != null ? ConsumedMessages.Any(filter) : ConsumedMessages.Any<T>(s => s.Context.Message.CorrelationId == message.CorrelationId))).ShouldBeTrue();
    }
}