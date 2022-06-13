using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using MassTransit.Testing;
using MassTransit.Testing.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
	/// <summary>
	/// Test class for consuming a IExecuteOrder message and publish an IOrder Mqtt message with proper bus configuration
	/// </summary>
	[TestClass]
	public class ExecuteTransportOrderConsumerProdTest
	{
		/// <summary>
		/// Test consuming a IExecuteOrder message and publish an IOrder Mqtt message with proper bus configuration
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task TestTransportOrderConsumer()
		{
			var collection = new ServiceCollection()
				.AddMassTransitTestHarness(x =>
				{
					x.AddConsumer<ExecuteTransportOrderConsumer>();
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

			//Create a publish observer
			var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
			using (testHarness.Bus.ConnectPublishObserver(publishObserver))
			{
				//Publish IExecuteTransportOrder
				await testHarness.Bus.Publish<IExecuteTransportOrder>(new { AssignedAgv = "agv0", Order = new Order("1234", "Pick1", "Drop5", "na", HostOrderCreator.Simulation) });
				//Check if sent
				Assert.IsTrue(await testHarness.Published.Any<IExecuteTransportOrder>());

				//Get Consumer Harness
				IConsumerTestHarness<ExecuteTransportOrderConsumer> consumerHarness = testHarness.GetConsumerHarness<ExecuteTransportOrderConsumer>();
				//Check if consumed
				Assert.IsTrue(await consumerHarness.Consumed.Any<IExecuteTransportOrder>());
				//Get consumed message
				IReceivedMessage<IExecuteTransportOrder> consumedMessage = consumerHarness.Consumed.Select<IExecuteTransportOrder>().First();
				//Check consumed message
				Assert.AreEqual(consumedMessage.Context.Message.AssignedAgv, "agv0");
				Assert.AreEqual(consumedMessage.Context.Message.Order.OrderId, "1234");
				Assert.AreEqual(consumedMessage.Context.Message.Order.Source, "Pick1");

				//Get published IOrder Mqtt message
				IPublishedMessage<IOrder> a = publishObserver.Messages.Select<IOrder>().First();
				//Check routing key and content type
				var routingKeySpecified = typeof(IOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
				Assert.AreEqual(routingKeySpecified?.RoutingKeyValue, a.Context.RoutingKey());
				Assert.AreEqual(SystemTextJsonRawMessageSerializer.JsonContentType, a.Context.ContentType);

				//Check content
				Assert.AreEqual(a.Context.Message.Identifier, "agv0");
				Assert.AreEqual(a.Context.Message.OrderId, "1234");
				Assert.AreEqual(a.Context.Message.Stations?.FromStationIdentifier, "Pick1");
				Assert.AreEqual(a.Context.Message.Stations?.ToStationIdentifier, "Drop5");
			}
		}
	}
}
