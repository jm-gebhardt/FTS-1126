using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain;

[TestClass]
public class OrderProfileTest
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void SetUp()
    {
        _mapper = MappingHelper.GetMapper(new OrderProfile(), new AgvActionProfile());
    }

    [TestMethod]
    public void Map_OrderToMqttOrder_ShouldMapAllProperties()
    {
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 3, AgvGenerator.Generate(Guid.NewGuid()));

        var mqttOrder = _mapper.Map<MqttOrder>(order);

        mqttOrder.OrderId.ShouldBe(order.OrderId);
    }
}