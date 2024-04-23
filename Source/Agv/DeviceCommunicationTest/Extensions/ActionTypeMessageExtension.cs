using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.DeviceCommunication.Extensions;
using Gebhardt.Acc.DeviceCommunication.Messages.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Extensions;

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