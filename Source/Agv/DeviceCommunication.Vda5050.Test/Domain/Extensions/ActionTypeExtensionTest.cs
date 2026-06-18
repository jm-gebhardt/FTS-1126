using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class ActionTypeExtensionTest
{
    [TestMethod]
    public void MatchesVda5050Type_PickType_MatchesPickString()
    {
        ActionType.Pick.MatchesVda5050Type(Vda5050Specifications.ActionTypes.Pick).ShouldBeTrue();
    }

    [TestMethod]
    public void MatchesVda5050Type_DropType_MatchesDropString()
    {
        ActionType.Drop.MatchesVda5050Type(Vda5050Specifications.ActionTypes.Drop).ShouldBeTrue();
    }

    [TestMethod]
    public void MatchesVda5050Type_ChargeType_MatchesStartChargingString()
    {
        ActionType.Charge.MatchesVda5050Type(Vda5050Specifications.ActionTypes.StartCharging).ShouldBeTrue();
    }

    [TestMethod]
    public void MatchesVda5050Type_PickType_DoesNotMatchDropString()
    {
        ActionType.Pick.MatchesVda5050Type(Vda5050Specifications.ActionTypes.Drop).ShouldBeFalse();
    }

    [TestMethod]
    public void MatchesVda5050Type_DropType_DoesNotMatchPickString()
    {
        ActionType.Drop.MatchesVda5050Type(Vda5050Specifications.ActionTypes.Pick).ShouldBeFalse();
    }

    [TestMethod]
    public void MatchesVda5050Type_ChargeType_DoesNotMatchPickString()
    {
        ActionType.Charge.MatchesVda5050Type(Vda5050Specifications.ActionTypes.Pick).ShouldBeFalse();
    }

    [TestMethod]
    public void MatchesVda5050Type_PickType_DoesNotMatchStartChargingString()
    {
        ActionType.Pick.MatchesVda5050Type(Vda5050Specifications.ActionTypes.StartCharging).ShouldBeFalse();
    }

    [TestMethod]
    public void MatchesVda5050Type_AnyType_DoesNotMatchUnknownString()
    {
        ActionType.Pick.MatchesVda5050Type("unknownAction").ShouldBeFalse();
        ActionType.Drop.MatchesVda5050Type("unknownAction").ShouldBeFalse();
        ActionType.Charge.MatchesVda5050Type("unknownAction").ShouldBeFalse();
    }

    [TestMethod]
    public void MatchesVda5050Type_MoveType_ReturnsFalseForAllVda5050Types()
    {
        ActionType.Move.MatchesVda5050Type(Vda5050Specifications.ActionTypes.Pick).ShouldBeFalse();
        ActionType.Move.MatchesVda5050Type(Vda5050Specifications.ActionTypes.Drop).ShouldBeFalse();
        ActionType.Move.MatchesVda5050Type(Vda5050Specifications.ActionTypes.StartCharging).ShouldBeFalse();
    }
}
