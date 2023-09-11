using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;

/// <summary>
/// Provides a test fixture for the <see cref="HostOrderStateMachine"/>.
/// </summary>
public class HostOrderStateMachineTestFixture: BaseStateMachineTestFixture<HostOrderStateMachine, HostOrderSaga>
{
    /// <summary>
    /// Initializes a new <see cref="HostOrderStateMachineTestFixture"/> instance.
    /// </summary>
    public HostOrderStateMachineTestFixture(
        Action<IServiceCollection>? services = null,
        Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        TimeSpan? inactivityTimeout = null)
        : base(services, busRegistrationConfigurator, inMemoryConfiguration, inactivityTimeout)
    {
        ConfigureServices();
        Build();
    }

    private void ConfigureServices()
    {
        ServiceCollection.AddSingleton<IStateObserver<HostOrderSaga>, HostOrderStateObserver>();
    }
}