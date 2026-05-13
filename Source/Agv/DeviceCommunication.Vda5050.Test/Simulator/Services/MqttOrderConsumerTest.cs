using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

[TestClass]
public class MqttOrderConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttOrder_NewOrder_PublishesStartOrder()
    {
        var fixture = new MqttOrderConsumerTestFixture();
        var message = MqttOrderGenerator.Generate(orderId: "order1", orderUpdateId: 0);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<StartOrder>();
        await fixture.ShouldNotHavePublishedAsync<UpdateOrder>();
    }

    [TestMethod]
    public async Task Consume_MqttOrder_NewOrder_StartOrderContainsCorrectOrderData()
    {
        var fixture = new MqttOrderConsumerTestFixture();
        var message = MqttOrderGenerator.Generate(orderId: "order1", orderUpdateId: 0);

        await fixture.PublishMqttMessageAsync(message);

        var published = await fixture.GetFirstPublishedMessageAsync<StartOrder>();
        published.Order.OrderId.ShouldBe("order1");
        published.Order.OrderUpdateId.ShouldBe(0);
    }

    [TestMethod]
    public async Task Consume_MqttOrder_OrderUpdate_PublishesUpdateOrder()
    {
        var fixture = new MqttOrderConsumerTestFixture();
        var message = MqttOrderGenerator.Generate(orderId: "order1", orderUpdateId: 1);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<UpdateOrder>();
        await fixture.ShouldNotHavePublishedAsync<StartOrder>();
    }

    [TestMethod]
    public async Task Consume_MqttOrder_OrderUpdate_UpdateOrderContainsCorrectOrderData()
    {
        var fixture = new MqttOrderConsumerTestFixture();
        var message = MqttOrderGenerator.Generate(orderId: "order1", orderUpdateId: 2);

        await fixture.PublishMqttMessageAsync(message);

        var published = await fixture.GetFirstPublishedMessageAsync<UpdateOrder>();
        published.Order.OrderId.ShouldBe("order1");
        published.Order.OrderUpdateId.ShouldBe(2);
    }
}
