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
        public readonly ServiceProvider Provider;

        /// <summary>
        /// Started TestHarness
        /// </summary>
        public readonly ITestHarness TestHarness;

        /// <summary>
        /// Consumer Hardness
        /// </summary>
        public readonly IConsumerTestHarness<TConsumer> ConsumerHarness;

        /// <summary>
        /// Consumer test fixture to be used for consumer unit/integration tests
        /// </summary>
        /// <param name="services">Service collection instance with MassTransitTestHarness added</param>
        /// <param name="busRegistrationConfigurator">MassTransit registration with consumer already added</param>
        /// <param name="inMemoryConfiguration">MassTransit bus configurator</param>
        public MassTransitTestFixture(
            Action<IServiceCollection>? services = null,
            Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
            Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null)
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

            services?.Invoke(collection);

            Provider = collection.BuildServiceProvider(true);
            TestHarness = Provider.GetTestHarness();
            TestHarness.Start();
            ConsumerHarness = TestHarness.GetConsumerHarness<TConsumer>();
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