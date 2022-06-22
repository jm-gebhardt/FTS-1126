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
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
	/// <summary>
	/// Test class for consuming an IExecuteOrder message and publish an IOrder Mqtt message with proper bus configuration
	/// </summary>
	[TestClass]
	public class ExecuteTransportOrderConsumerProdTest
	{
		/// <summary>
		/// Test consuming a IExecuteOrder message and publish an IOrder Mqtt message with proper bus configuration
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task Test_Send_TransportOrder_To_AGV_Via_MQTT()
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
				const string agvId = "agv0";
				const string orderId = "1234";
				const string fromStationId = "Pick1"; 
				const string toStationId = "Drop5";
				const string leNo = "na";

				//Publish IExecuteTransportOrder
				await testHarness.Bus.Publish<IExecuteTransportOrder>(
					new
					{
						AssignedAgv = agvId,
						Order = new Order(orderId,
							fromStationId,
							toStationId,
							leNo,
							HostOrderCreator.Simulation)
					});
				//Check if sent
				Assert.IsTrue(await testHarness.Published.Any<IExecuteTransportOrder>());

				//Get Consumer Harness
				IConsumerTestHarness<ExecuteTransportOrderConsumer> consumerHarness = testHarness.GetConsumerHarness<ExecuteTransportOrderConsumer>();
				//Check if consumed
				Assert.IsTrue(await consumerHarness.Consumed.Any<IExecuteTransportOrder>());
				//Get consumed message
				IReceivedMessage<IExecuteTransportOrder> consumedMessage = consumerHarness.Consumed.Select<IExecuteTransportOrder>().First();
				//Check consumed message
				Assert.AreEqual(agvId, consumedMessage.Context.Message.AssignedAgv);
				Assert.AreEqual(orderId, consumedMessage.Context.Message.Order.OrderId);
				Assert.AreEqual(fromStationId, consumedMessage.Context.Message.Order.Source);
				
				//Get published IOrder Mqtt message
				IPublishedMessage<IOrder> a = publishObserver.Messages.Select<IOrder>().First();
				//Check routing key and content type
				var routingKeySpecified = typeof(IOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
				Assert.AreEqual(routingKeySpecified?.RoutingKeyValue, a.Context.RoutingKey());
				Assert.AreEqual(SystemTextJsonRawMessageSerializer.JsonContentType, a.Context.ContentType);

				//Check content
				Assert.AreEqual(agvId, a.Context.Message.Identifier);

				Assert.AreEqual(fromStationId, a.Context.Message.Stations?.FromStationIdentifier);
				Assert.AreEqual(toStationId, a.Context.Message.Stations?.ToStationIdentifier);
				Assert.AreEqual(OrderType.Transport, a.Context.Message.OrderType);
				Assert.AreEqual(OrderRecordType.Order, a.Context.Message.RecordType);
			}
		}

		/// <summary>
		/// Test consuming an ISendAgvChargeOrder message and publish an IOrder Mqtt message with proper bus configuration
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task Test_Send_ChargeOrder_To_AGV_Via_MQTT()
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
				const string agvId = "agv0";
				const string chargerId = "CHG1";
				//Publish IExecuteTransportOrder
				await testHarness.Bus.Publish<ISendAgvChargeOrder>(new { CorrelationId = Guid.NewGuid(), AgvId = agvId, ToCharger = chargerId });
				//Check if sent
				Assert.IsTrue(await testHarness.Published.Any<ISendAgvChargeOrder>());

				//Get Consumer Harness
				IConsumerTestHarness<ExecuteTransportOrderConsumer> consumerHarness = testHarness.GetConsumerHarness<ExecuteTransportOrderConsumer>();
				//Check if consumed
				Assert.IsTrue(await consumerHarness.Consumed.Any<ISendAgvChargeOrder>());
				//Get consumed message
				IReceivedMessage<ISendAgvChargeOrder> consumedMessage = consumerHarness.Consumed.Select<ISendAgvChargeOrder>().First();
				//Check consumed message
				Assert.AreEqual(agvId, consumedMessage.Context.Message.AgvId);
				Assert.AreEqual(chargerId, consumedMessage.Context.Message.ToCharger);
				Guid? guid = consumedMessage.Context.CorrelationId;

				//Get published IOrder Mqtt message
				IPublishedMessage<IOrder> a = publishObserver.Messages.Select<IOrder>().First();
				//Check routing key and content type
				var routingKeySpecified = typeof(IOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
				Assert.AreEqual(routingKeySpecified?.RoutingKeyValue, a.Context.RoutingKey());
				Assert.AreEqual(SystemTextJsonRawMessageSerializer.JsonContentType, a.Context.ContentType);

				//Check content
				Assert.AreEqual(agvId, a.Context.Message.Identifier);
				Assert.AreEqual(guid.ToString(), a.Context.Message.OrderId);
				Assert.AreEqual(chargerId, a.Context.Message.Stations?.ToStationIdentifier);
			}
		}
	}
}
