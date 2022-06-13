using Gebhardt.StoreWare.Agv.Common.Consumer;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Extensions
{
	/// <summary>
	/// A bus configurator extension that allows to register a Mqtt consumer. The specific consumer definition is generated automatically.
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public static class MqttBusConfiguratorExtension
    {
		/// <summary>
		/// Add a Mqtt consumer
		/// </summary>
		/// <param name="configurator"></param>
		[JetBrains.Annotations.PublicAPI]
		public static void AddMqttConsumer<T>(this IBusRegistrationConfigurator configurator) where T : class, IConsumer
	    {
		    IEnumerable<Type> consumerTypes = new[] { typeof(T), typeof(BaseMqttConsumerDefinition<T>) };
		    configurator.AddConsumers(consumerTypes.ToArray());
		}
	}
}
