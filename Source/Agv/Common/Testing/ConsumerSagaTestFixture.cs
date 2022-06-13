using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using MassTransit.Testing;


namespace Gebhardt.StoreWare.Agv.Common.Testing
{
    /// <summary>
    /// Testing fixture to test MassTransit consumer sagas.
    /// </summary>
    /// <typeparam name="TConsumerSaga"></typeparam>
    [JetBrains.Annotations.PublicAPI]
    public class ConsumerSagaTestFixture<TConsumerSaga>
        where TConsumerSaga : class, ISaga
    {
        /// <summary>
        /// Service provider.
        /// </summary>
        public readonly ServiceProvider Provider;

        /// <summary>
        /// Test Harness
        /// </summary>
        public readonly ITestHarness TestHarness;

        /// <summary>
        /// Saga test harness.
        /// </summary>
        public readonly ISagaTestHarness<TConsumerSaga> SagaHarness;


        /// <summary>
        /// Consumer saga test fixture to be used for unit/integration tests.
        /// </summary>
        /// <param name="services">IServiceCollection action</param>
        /// <param name="busRegistrationConfigurator">IBusRegistrationConfigurator action</param>
        public ConsumerSagaTestFixture(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
        {
            var collection = new ServiceCollection()
                .AddMassTransitTestHarness(x =>
                {
                    x.AddSaga<TConsumerSaga>().InMemoryRepository();
                    busRegistrationConfigurator?.Invoke(x);
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });

            services?.Invoke(collection);

            Provider = collection.BuildServiceProvider(true);
            TestHarness = Provider.GetTestHarness();
            TestHarness.Start();
            SagaHarness = TestHarness.GetSagaHarness<TConsumerSaga>();
        }
    }
}