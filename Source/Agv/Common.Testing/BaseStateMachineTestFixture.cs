using MassTransit;
using MassTransit.Saga;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Abstract base class for testing MassTransit StateMachines
/// </summary>
public abstract class BaseStateMachineTestFixture<TStateMachine, TInstance>
    where TStateMachine : class, SagaStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// A registered Saga AgvState Machine 
    /// </summary>
    public TStateMachine Machine = null!;

    /// <summary>
    /// Service Collection
    /// </summary>
    private readonly IServiceCollection _serviceCollection;

    /// <summary>
    /// Service Provider
    /// </summary>
    public ServiceProvider Provider = null!;

    /// <summary>
    /// Saga AgvState Machine test harness
    /// </summary>
    public ISagaStateMachineTestHarness<TStateMachine, TInstance> SagaHarness = null!;

    /// <summary>
    /// Test Harness
    /// </summary>
    public ITestHarness TestHarness = null!;

    /// <summary>
    /// Saga Dictionary. This can be used to manipulate an OrderSaga directly.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public IndexedSagaDictionary<TInstance> SagaDictionary = null!;
    
    private readonly TimeSpan? _inactivityTimeout;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="services"></param>
    /// <param name="busRegistrationConfigurator"></param>
    /// <param name="inMemoryConfiguration"></param>
    /// <param name="inactivityTimeout"></param>
    protected BaseStateMachineTestFixture(
        Action<IServiceCollection>? services = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        TimeSpan? inactivityTimeout = null)
    {
        _serviceCollection = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<TStateMachine, TInstance>()
                    .InMemoryRepository();
                busRegistrationConfigurator?.Invoke(x);
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                    inMemoryConfiguration?.Invoke(context, cfg);
                });
            });

        services?.Invoke(_serviceCollection);
        _inactivityTimeout = inactivityTimeout;
    }

    /// <summary>
    /// This must be called from child to build the test fixture
    /// </summary>
    protected void Build()
    {
        Provider = _serviceCollection.BuildServiceProvider(true);
        TestHarness = Provider.GetTestHarness();
        if (_inactivityTimeout.HasValue)
        {
            TestHarness.TestInactivityTimeout = _inactivityTimeout.Value;
        }
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