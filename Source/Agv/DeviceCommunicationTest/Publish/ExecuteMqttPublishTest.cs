using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using MassTransit.Testing;
using MassTransit.Testing.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Publish
{
	/// <summary>
	/// Test class for publishing a MQTT message with proper bus configuration
	/// </summary>
	[TestClass]
	public class ExecuteMqttPublishTest
	{
		/// <summary>
		/// Test publishing a MQTT message with proper bus configuration
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task TestPublishMqttMessageAndBusConfiguration()
		{
			var collection = new ServiceCollection()
					.AddMassTransitTestHarness(x =>
					{
						x.UsingInMemory((context, cfg) =>
						{
							cfg.ConfigureEndpoints(context);
							var serializerFactory = new SystemTextJsonRawMessageSerializerFactory();
							cfg.AddSerializer(serializerFactory);
							cfg.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
						});
					});

			var provider = collection.BuildServiceProvider(true);
			var testHarness = provider.GetTestHarness();
			await testHarness.Start();

			var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));

			using (testHarness.Bus.ConnectPublishObserver(publishObserver))
			{
				await testHarness.Bus.PublishMqtt<IState>(new { OrderId = "1234", OperatingMode = "AUTOMATIC"}, null);

				IPublishedMessage<IState> a = publishObserver.Messages.Select<IState>().First();

				var routingKeySpecified = typeof(IState).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;

				Assert.AreEqual(routingKeySpecified?.RoutingKeyValue, a.Context.RoutingKey());
				Assert.AreEqual(SystemTextJsonRawMessageSerializer.JsonContentType, a.Context.ContentType);
				Assert.AreEqual(a.Context.Message.OrderId, "1234");
				Assert.AreEqual(a.Context.Message.OperatingMode, OperatingMode.Automatic);
			}
		}
	}
}
