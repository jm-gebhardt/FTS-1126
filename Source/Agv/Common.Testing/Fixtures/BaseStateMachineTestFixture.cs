using MassTransit;
using MassTransit.Saga;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

/// <summary>
/// Abstract base class for testing MassTransit state machines
/// </summary>
public abstract class BaseStateMachineTestFixture<TStateMachine, TInstance> : MassTransitTestFixture
    where TStateMachine : class, SagaStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    private readonly TimeSpan? _inactivityTimeout;

    /// <summary>
    /// A registered Saga AgvState Machine 
    /// </summary>
    protected TStateMachine Machine { get; private set; } = null!;

    /// <summary>
    /// Saga AgvState Machine test harness
    /// </summary>
    public ISagaStateMachineTestHarness<TStateMachine, TInstance> SagaHarness { get; private set; } = null!;

    protected override IReceivedMessageList ConsumedMessages => SagaHarness.Consumed;

    /// <summary>
    /// Saga Dictionary. This can be used to manipulate an OrderSaga directly.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public IndexedSagaDictionary<TInstance> SagaDictionary { get; private set; } = null!;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="inactivityTimeout"></param>
    protected BaseStateMachineTestFixture(TimeSpan? inactivityTimeout = null)
    {
        _inactivityTimeout = inactivityTimeout;
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddSagaStateMachine<TStateMachine, TInstance>()
            .InMemoryRepository();
    }

    /// <summary>
    /// This must be called from child to build the test fixture
    /// </summary>
    protected new void Build()
    {
        base.Build();
        if (_inactivityTimeout.HasValue)
        {
            TestHarness.TestInactivityTimeout = _inactivityTimeout.Value;
        }
        SagaHarness = TestHarness.GetSagaStateMachineHarness<TStateMachine, TInstance>();
        Machine = SagaHarness.StateMachine;
        SagaDictionary = Provider.GetRequiredService<IndexedSagaDictionary<TInstance>>();
    }

    /// <summary>
    /// Add TInstance to saga dictionary
    /// This can be used to add a Saga with a predefined state
    /// </summary>
    /// <param name="instance">Saga Instance</param>
    protected void Add(TInstance instance)
    {
        SagaDictionary.Add(new SagaInstance<TInstance>(instance));
    }
}