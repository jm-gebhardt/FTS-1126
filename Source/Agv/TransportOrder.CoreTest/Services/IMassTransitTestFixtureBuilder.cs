using Gebhardt.StoreWare.Agv.Common.Testing;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services;

public interface IMassTransitTestFixtureBuilder<out TBuiltType, out TBuilder> : ITestFixtureBuilder<TBuiltType> where TBuiltType : class
{
    TBuilder SetupBusRegistrationConfigurator(Action<IBusRegistrationConfigurator> busRegistrationConfigurator);

}