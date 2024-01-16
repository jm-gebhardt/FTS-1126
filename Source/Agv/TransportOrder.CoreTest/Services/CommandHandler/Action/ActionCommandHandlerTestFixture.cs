using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

public class ActionCommandHandlerTestFixture(
    TimeSpan? inactivityTimeout = null,
    Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
    Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
    params Action<IServiceCollection>[] services)
    : ActionGroupTestFixtureBase<ActionCommandHandler>(inactivityTimeout, busRegistrationConfigurator, inMemoryConfiguration, services);