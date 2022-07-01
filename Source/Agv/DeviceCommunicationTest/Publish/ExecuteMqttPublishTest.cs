using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using MassTransit;
using MassTransit.Serialization;
using MassTransit.Testing;
using MassTransit.Testing.Implementations;
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
			var fixture = new MqttTestFixture();

			var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));

			using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
			{
				await fixture.TestHarness.Bus.PublishMqtt<IState>(new { OrderId = "1234", OperatingMode = "AUTOMATIC"}, null);

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
