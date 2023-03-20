using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services
{
    public class CommandConsumerTestFixture<TCommand, TCommandConsumer> : MediatorTestFixture<TCommand, TCommandConsumer>
        where TCommandConsumer : class, IConsumer
        where TCommand : class
    {
        protected IAgvRegistry AgvRegistry { get; }

        protected CommandConsumerTestFixture()
        {
            var agvRegistryTestFixture = new AgvRegistryTestFixture();
            AgvRegistry = agvRegistryTestFixture.AgvRegistry;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        protected CommandConsumerTestFixture<TCommand, TCommandConsumer> UseDefaultServices()
        {
            AddServices(sc => sc.AddSingleton(_ => AgvRegistry));
            return this;
        }
        
        // ReSharper disable once UnusedMember.Global
        protected CommandConsumerTestFixture<TCommand, TCommandConsumer> AddAgvRegistry(IAgvRegistry agvRegistry)
        {
            AddServices(sc => sc.AddSingleton(_ => agvRegistry));
            return this;
        }
    }
}