using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Testing fixture to test MassTransit StateMachines
/// </summary>
/// <typeparam name="TStateMachine"></typeparam>
/// <typeparam name="TInstance"></typeparam>
[PublicAPI]
public class StateMachineTestFixture<TStateMachine, TInstance> : BaseStateMachineTestFixture<TStateMachine, TInstance>
    where TStateMachine : class, SagaStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <inheritdoc />
    public StateMachineTestFixture(
        Action<IServiceCollection>? services = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        TimeSpan? inactivityTimeout = null) : base(services, busRegistrationConfigurator, inMemoryConfiguration, inactivityTimeout)
    {
        Build();
    }
}