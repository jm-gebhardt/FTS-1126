using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi
{
    /// <summary>
    /// IBusRegistrationExtensions extensions
    /// </summary>
    public static class WebApiBusRegistrationExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="busRegistrationConfigurator"></param>
        /// <returns></returns>
        public static void AddWebApiConsumer(this IBusRegistrationConfigurator busRegistrationConfigurator)
        {
            busRegistrationConfigurator.AddConsumer<ActionGroupCreatedConsumer>();
            busRegistrationConfigurator.AddConsumer<ActionGroupChangedConsumer>();
            busRegistrationConfigurator.AddConsumer<AgvPositionChangedConsumer>();
            busRegistrationConfigurator.AddConsumer<AgvPropertyChangedConsumer>();
            busRegistrationConfigurator.AddConsumer<AgvStateChangedConsumer>();
        }
    }
}
