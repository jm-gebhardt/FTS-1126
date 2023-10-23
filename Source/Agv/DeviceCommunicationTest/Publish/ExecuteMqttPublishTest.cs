using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using MassTransit;
using MassTransit.Serialization;
using MassTransit.Testing;
using MassTransit.Testing.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Publish;

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

        var routingKeySpecified = typeof(MqttState).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
        const string serialNumber = "AGV01";
        const string orderId = "1234";

        using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
        {
            await fixture.TestHarness.Bus.PublishMqtt(new MqttState(serialNumber, orderId) { MqttOperatingMode = MqttOperatingMode.Automatic }, null);

            IPublishedMessage<MqttState> publishedMessage = publishObserver.Messages.Select<MqttState>().First();

            publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
            publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);
            publishedMessage.Context.Message.SerialNumber.ShouldBe(serialNumber);
            publishedMessage.Context.Message.OrderId.ShouldBe(orderId);
            publishedMessage.Context.Message.MqttOperatingMode.ShouldBe(MqttOperatingMode.Automatic);
        }
    }
}