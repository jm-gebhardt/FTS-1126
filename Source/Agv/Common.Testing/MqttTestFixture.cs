using Gebhardt.StoreWare.Agv.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Testing fixture to test MQTT consumers.
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
[PublicAPI]
public class MqttConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
{
    private readonly List<Action<IServiceCollection>> _serviceActions = new();
        
    /// <summary>
    /// Service provider.
    /// </summary>
    public ServiceProvider Provider = null!;

    /// <summary>
    /// Test harness.
    /// </summary>
    public ITestHarness TestHarness = null!;

    /// <summary>
    /// Consumer test harness.
    /// </summary>
    public IConsumerTestHarness<TConsumer> ConsumerHarness = null!;

    /// <summary>
    /// constructor for consumer saga test fixture to be used for unit/integration test
    /// </summary>
    /// <param name="services"></param>
    /// <param name="busRegistrationConfigurator"></param>
    public MqttConsumerTestFixture(
        Action<IServiceCollection>? services = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
    {
        Build(services, busRegistrationConfigurator);
    }

    /// <summary>
    /// Consumer saga test fixture to be used for unit/integration tests.
    /// </summary>
    /// <param name="services">IServiceCollection action</param>
    /// <param name="busRegistrationConfigurator">IBusRegistrationConfigurator action</param>
    public MqttConsumerTestFixture<TConsumer> Build(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
    {
        var collection = new ServiceCollection().
            AddMassTransitTestHarness(x =>
            {
                x.AddMqttConsumer<TConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.AddSerializer(new SystemTextJsonRawMessageSerializerFactory());
                    cfg.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
                    cfg.ConfigureEndpoints(context);
                });
            });

        if (services != null)
        {
            _serviceActions.Add(services);
        }
            
        foreach (Action<IServiceCollection> service in _serviceActions)
        {
            service.Invoke(collection);
        }

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
    public MqttConsumerTestFixture<TConsumer> AddServices(Action<IServiceCollection> services)
    {
        _serviceActions.Add(services);
        return this;
    }
}

/// <summary>
/// Testing fixture to test MQTT.
/// </summary>
[PublicAPI]
public class MqttTestFixture
{
    /// <summary>
    /// Service provider.
    /// </summary>
    public readonly ServiceProvider Provider;

    /// <summary>
    /// Test harness.
    /// </summary>
    public readonly ITestHarness TestHarness;

    /// <summary>
    /// Consumer saga test fixture to be used for unit/integration tests.
    /// </summary>
    /// <param name="services">IServiceCollection action</param>
    /// <param name="busRegistrationConfigurator">IBusRegistrationConfigurator action</param>
    public MqttTestFixture(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
    {
        var collection = new ServiceCollection().
            AddMassTransitTestHarness(x =>
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.AddSerializer(new SystemTextJsonRawMessageSerializerFactory());
                    cfg.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
                    cfg.ConfigureEndpoints(context);
                });
            });

        services?.Invoke(collection);

        Provider = collection.BuildServiceProvider(true);
        TestHarness = Provider.GetTestHarness();
        TestHarness.Start();
    }
}