using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Extensions;

[TestClass]
public class ActionTypeMessageExtension
{
    [TestMethod]
    public void ToOrderType_ValidData_Converted()
    {
        foreach (ActionType ordinal in Enum.GetValues(typeof(ActionType)))
        {
            ordinal.ToOrderType().ShouldBe(Enum.Parse<OrderType>(ordinal.ToString()));
        }
    }
}