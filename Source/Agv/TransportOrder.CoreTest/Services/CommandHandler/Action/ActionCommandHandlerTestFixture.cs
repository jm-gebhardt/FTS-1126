using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Action;

public class ActionCommandHandlerTestFixture : ActionGroupTestFixtureBase<ActionCommandHandler>
{
    public ActionCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
        params Action<IServiceCollection>[] services) : base(inactivityTimeout, busRegistrationConfigurator, inMemoryConfiguration, services)
    {
    }
}