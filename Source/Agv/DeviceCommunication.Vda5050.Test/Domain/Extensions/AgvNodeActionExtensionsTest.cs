using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Shouldly;
using static Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class AgvNodeActionExtensionsTest
{
    [TestMethod]
    public void InferNodeType_PickOnly_ReturnsPick()
    {
        ICollection<AgvNodeAction> actions = [new() { ActionType = "pick" }];

        object result = actions.InferNodeType();

        result.ShouldBe(NodeType.Pick);
    }

    [TestMethod]
    public void InferNodeType_DropOnly_ReturnsDrop()
    {
        ICollection<AgvNodeAction> actions = [new() { ActionType = "drop" }];

        object result = actions.InferNodeType();

        result.ShouldBe(NodeType.Drop);
    }

    [TestMethod]
    public void InferNodeType_PickAndDrop_ReturnsPickDrop()
    {
        ICollection<AgvNodeAction> actions =
        [
            new() { ActionType = "pick" },
            new() { ActionType = "drop" }
        ];

        object result = actions.InferNodeType();

        result.ShouldBe(NodeType.PickDrop);
    }

    [TestMethod]
    public void InferNodeType_StartChargingOnly_ReturnsCharge()
    {
        ICollection<AgvNodeAction> actions = [new() { ActionType = "startCharging" }];

        object result = actions.InferNodeType();

        result.ShouldBe(NodeType.Charge);
    }

    [TestMethod]
    public void InferNodeType_PickAndStartCharging_ReturnsList()
    {
        ICollection<AgvNodeAction> actions =
        [
            new() { ActionType = "pick" },
            new() { ActionType = "startCharging" }
        ];

        object result = actions.InferNodeType();

        var list = result.ShouldBeOfType<List<NodeType>>();
        list.ShouldContain(NodeType.Pick);
        list.ShouldContain(NodeType.Charge);
    }

    [TestMethod]
    public void InferNodeType_NoActions_ReturnsWayPoint()
    {
        ICollection<AgvNodeAction> actions = [];

        object result = actions.InferNodeType();

        result.ShouldBe(NodeType.WayPoint);
    }

    [TestMethod]
    public void InferNodeType_UnknownActionType_ReturnsWayPoint()
    {
        ICollection<AgvNodeAction> actions = [new() { ActionType = "customAction" }];

        object result = actions.InferNodeType();

        result.ShouldBe(NodeType.WayPoint);
    }

    [TestMethod]
    public void InferNodeType_PickDropAndCharge_ReturnsList()
    {
        ICollection<AgvNodeAction> actions =
        [
            new() { ActionType = "pick" },
            new() { ActionType = "drop" },
            new() { ActionType = "startCharging" }
        ];

        object result = actions.InferNodeType();

        var list = result.ShouldBeOfType<List<NodeType>>();
        list.ShouldContain(NodeType.PickDrop);
        list.ShouldContain(NodeType.Charge);
    }
}