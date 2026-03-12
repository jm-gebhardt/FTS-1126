using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services;

[TestClass]
public class OrderEventHandlerTest
{
    [TestMethod]
    public async Task Consume_OrderStateReceived_ValidData_PublishesVehicleActionStatusChanged()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var orderState = OrderGenerator.GenerateOrderState("orderId", 0, 0, false, actionStates: [new OrderActionState() { ActionId = "id", ActionStatus = VehicleActionStatus.Initializing}]);
        var fixture = new OrderEventHandlerTestFixture()
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new OrderStateReceived(Guid.NewGuid(), orderState));

        await fixture.ShouldHavePublishedAsync<VehicleActionStatusChanged>();
    }
}