using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.RequestResponseHandler;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;

/// <summary>
/// Provides extensions methods to <see cref="IMediatorRegistrationConfigurator"/>.
/// </summary>
public static class MediatorRegistrationExtension
{
    extension(IMediatorRegistrationConfigurator configurator)
    {
        /// <summary>
        /// Add the mediator
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IMediatorRegistrationConfigurator AddMediator()
        {
            configurator.AddConsumer<SimulatedAgvRequestHandler>();
            configurator.AddRequestClient<RequestOperatingModeSwitch>();
            configurator.AddRequestClient<RequestSetToPosition>();
            return configurator;
        }
    }
}