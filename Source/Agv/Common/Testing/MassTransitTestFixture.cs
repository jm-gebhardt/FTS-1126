using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Testing
{
    /// <summary>
    /// Testing fixture to test MassTransit Consumers
    /// </summary>
    /// <typeparam name="TConsumer"></typeparam>
    [PublicAPI]
    public class MassTransitTestFixture<TConsumer> where TConsumer : class, IConsumer
    {
        /// <summary>
        /// Service provider
        /// </summary>
        public ServiceProvider? Provider;

        /// <summary>
        /// Started TestHarness
        /// </summary>
        public ITestHarness? TestHarness;

        /// <summary>
        /// Consumer Hardness
        /// </summary>
        public IConsumerTestHarness<TConsumer>? ConsumerHarness;

        /// <summary>
        /// Consumer test fixture to be used for consumer unit/integration tests
        /// </summary>
        /// <param name="services">Service collection instance with MassTransitTestHarness added</param>
        /// <param name="busRegistrationConfigurator">MassTransit registration with consumer already added</param>
        /// <param name="inMemoryConfiguration">MassTransit bus configurator</param>
        public MassTransitTestFixture(
            Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
            Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
            params Action<IServiceCollection>[] services)
        {
            Build(busRegistrationConfigurator, inMemoryConfiguration, services);
        }

        /// <inheritdoc cref="Build(System.Action{Microsoft.Extensions.DependencyInjection.IServiceCollection}[])"/>
        protected MassTransitTestFixture<TConsumer> Build(params Action<IServiceCollection>[] services) => Build(null, null, services);

        /// <summary>
        /// Builds the service provider based on the service collection, in order to be able to register services lazy
        /// </summary>
        /// <param name="services"></param>
        /// <param name="busRegistrationConfigurator"></param>
        /// <param name="inMemoryConfiguration"></param>
        protected MassTransitTestFixture<TConsumer> Build(Action<IBusRegistrationConfigurator>? busRegistrationConfigurator,
            Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration,
            params Action<IServiceCollection>[] services)
        {
            var collection = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TConsumer>();
                busRegistrationConfigurator?.Invoke(x);
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                    inMemoryConfiguration?.Invoke(context, cfg);
                });
            });

            foreach (Action<IServiceCollection> service in services)
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
        /// Is executed to configure the service collection
        /// </summary>
        /// <param name="collection"></param>
        protected virtual void ConfigureServices(IServiceCollection collection)
        {

        }
    }

    /// <summary>
    /// Testing fixture to test MassTransit
    /// </summary>
    [PublicAPI]
    public class MassTransitTestFixture
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
        public MassTransitTestFixture(
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
}