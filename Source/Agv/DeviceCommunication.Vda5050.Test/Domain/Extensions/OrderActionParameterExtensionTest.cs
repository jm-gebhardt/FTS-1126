using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class OrderActionParameterExtensionTest
{
    [TestMethod]
    public void AddIfRequired_RequiredParameter_AddsToCollection()
    {
        var agvAction = MakeAgvAction("pick", [new AgvActionParameter { Key = "station", IsOptional = false }]);
        ICollection<OrderActionParameter> parameters = [];

        parameters.AddIfRequired(agvAction, "station", "PS01");

        parameters.ShouldHaveSingleItem();
        parameters.Single().Key.ShouldBe("station");
        parameters.Single().Value.ShouldBe("PS01");
    }

    [TestMethod]
    public void AddIfRequired_OptionalParameter_DoesNotAdd()
    {
        var agvAction = MakeAgvAction("pick", [new AgvActionParameter { Key = "station", IsOptional = true }]);
        ICollection<OrderActionParameter> parameters = [];

        parameters.AddIfRequired(agvAction, "station", "PS01");

        parameters.ShouldBeEmpty();
    }

    [TestMethod]
    public void AddIfRequired_UndeclaredParameterKey_DoesNotAdd()
    {
        var agvAction = MakeAgvAction("pick", [new AgvActionParameter { Key = "otherKey", IsOptional = false }]);
        ICollection<OrderActionParameter> parameters = [];

        parameters.AddIfRequired(agvAction, "station", "PS01");

        parameters.ShouldBeEmpty();
    }

    [TestMethod]
    public void AddIfRequired_NoActionParameters_DoesNotAdd()
    {
        var agvAction = MakeAgvAction("pick", []);
        ICollection<OrderActionParameter> parameters = [];

        parameters.AddIfRequired(agvAction, "station", "PS01");

        parameters.ShouldBeEmpty();
    }

    [TestMethod]
    public void AddIfRequired_NullActionParameters_DoesNotAdd()
    {
        var agvAction = MakeAgvAction("pick", null);
        ICollection<OrderActionParameter> parameters = [];

        parameters.AddIfRequired(agvAction, "station", "PS01");

        parameters.ShouldBeEmpty();
    }

    [TestMethod]
    public void AddIfRequired_MultipleRequiredParameters_AddsAllRequired()
    {
        var agvAction = MakeAgvAction("pick",
        [
            new AgvActionParameter { Key = "station", IsOptional = false },
            new AgvActionParameter { Key = "lhd", IsOptional = false },
            new AgvActionParameter { Key = "optional", IsOptional = true }
        ]);
        ICollection<OrderActionParameter> parameters = [];

        parameters.AddIfRequired(agvAction, "station", "PS01");
        parameters.AddIfRequired(agvAction, "lhd", "device1");
        parameters.AddIfRequired(agvAction, "optional", "value");

        parameters.Count.ShouldBe(2);
        parameters.ShouldContain(p => p.Key == "station");
        parameters.ShouldContain(p => p.Key == "lhd");
    }

    private static AgvAction MakeAgvAction(string actionType, ICollection<AgvActionParameter>? parameters) =>
        new()
        {
            AgvSerialNumber = "AGV-001",
            ActionType = actionType,
            ActionParameters = parameters
        };
}
