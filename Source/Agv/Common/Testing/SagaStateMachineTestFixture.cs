using Microsoft.Extensions.DependencyInjection;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Saga;
using MassTransit.Testing;

namespace Gebhardt.StoreWare.Agv.Common.Testing
{
    /// <summary>
    /// Testing fixture to test MassTransit StateMachines
    /// </summary>
    /// <typeparam name="TStateMachine"></typeparam>
    /// <typeparam name="TInstance"></typeparam>
    [PublicAPI]
    public class StateMachineTestFixture<TStateMachine, TInstance>
        where TStateMachine : class, SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        /// <summary>
        /// A registered Saga AgvState Machine 
        /// </summary>
        public readonly TStateMachine Machine;
        /// <summary>
        /// Service Provider
        /// </summary>
        public readonly ServiceProvider Provider;
        /// <summary>
        /// Saga AgvState Machine test harness
        /// </summary>
        public readonly ISagaStateMachineTestHarness<TStateMachine, TInstance> SagaHarness;
        /// <summary>
        /// Test Harness
        /// </summary>
        public readonly ITestHarness TestHarness;
        /// <summary>
        /// Saga Dictionary. This can be used to manipulate an OrderSaga directly.
        /// </summary>
        public readonly IndexedSagaDictionary<TInstance> SagaDictionary;

        /// <summary>
        /// AgvState Machine Test Fixture to be used for StateMachine Unit/Integration tests
        /// </summary>
        /// <param name="services">IServiceCollection action</param>
        /// <param name="busRegistrationConfigurator">IBusRegistrationConfigurator action</param>
        public StateMachineTestFixture(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
        {
            var collection = new ServiceCollection()
                .AddMassTransitTestHarness(x =>
                {
                    x.AddSagaStateMachine<TStateMachine, TInstance>()
                        .InMemoryRepository();
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
            SagaHarness = TestHarness.GetSagaStateMachineHarness<TStateMachine, TInstance>();
            Machine = SagaHarness.StateMachine;
            SagaDictionary = Provider.GetRequiredService<IndexedSagaDictionary<TInstance>>();
        }

        /// <summary>
        /// Add TInstance to saga dictionary
        /// This can be used to add a Saga with a predefined state
        /// </summary>
        /// <param name="instance">Saga Instance</param>
        public void Add(TInstance instance)
        {
            SagaDictionary.Add(new SagaInstance<TInstance>(instance));
        }
    }
}
