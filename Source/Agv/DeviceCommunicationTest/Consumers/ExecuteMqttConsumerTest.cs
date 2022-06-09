using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using MassTransit.Configuration;
using MassTransit.Serialization;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
	/// <summary>
	/// Test class for consuming a MQTT message with proper bus configuration
	/// </summary>
	[TestClass]
	public class ExecuteMqttConsumerTest
	{
		/// <summary>
		/// Test consuming a MQTT message with proper bus configuration
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task TestConsumeMqttMessageAndBusConfiguration()
		{
			var collection = new ServiceCollection()
				.AddMassTransitTestHarness(x =>
				{
					x.AddMqttConsumer<AgvStateConsumer>();
					x.UsingInMemory((context, cfg) =>
					{
						var serializerFactory = new SystemTextJsonRawMessageSerializerFactory();
						cfg.AddSerializer(serializerFactory);
						cfg.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
						cfg.ConfigureEndpoints(context);
					});
				});
			var provider = collection?.BuildServiceProvider(true);
			var testHarness = provider.GetTestHarness();
			await testHarness.Start();
			IConsumerTestHarness<AgvStateConsumer> consumerHarness = testHarness.GetConsumerHarness<AgvStateConsumer>();

			await testHarness.Bus.PublishMqtt<IState>(new { OrderId = "1234", OperatingMode = "AUTOMATIC" }, null);

			Assert.IsTrue(await testHarness.Published.Any<IState>());
			Assert.IsTrue(await consumerHarness.Consumed.Any<IState>());
			IReceivedMessage<IState> consumedMessage = consumerHarness.Consumed.Select<IState>().First();
			Assert.AreEqual("1234", consumedMessage.Context.Message.OrderId);
			Assert.AreEqual(consumedMessage.Context.Message.OperatingMode, OperatingMode.Automatic);

		}
	}
}
