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

        ICollection<NodeType> result = actions.InferNodeType();

        result.ShouldHaveSingleItem().ShouldBe(NodeType.Pick);
    }

    [TestMethod]
    public void InferNodeType_DropOnly_ReturnsDrop()
    {
        ICollection<AgvNodeAction> actions = [new() { ActionType = "drop" }];

        ICollection<NodeType> result = actions.InferNodeType();

        result.ShouldHaveSingleItem().ShouldBe(NodeType.Drop);
    }

    [TestMethod]
    public void InferNodeType_PickAndDrop_ReturnsPickDrop()
    {
        ICollection<AgvNodeAction> actions =
        [
            new() { ActionType = "pick" },
            new() { ActionType = "drop" }
        ];

        ICollection<NodeType> result = actions.InferNodeType();

        result.ShouldHaveSingleItem().ShouldBe(NodeType.PickDrop);
    }

    [TestMethod]
    public void InferNodeType_StartChargingOnly_ReturnsCharge()
    {
        ICollection<AgvNodeAction> actions = [new() { ActionType = "startCharging" }];

        ICollection<NodeType> result = actions.InferNodeType();

        result.ShouldHaveSingleItem().ShouldBe(NodeType.Charge);
    }

    [TestMethod]
    public void InferNodeType_PickAndStartCharging_ReturnsList()
    {
        ICollection<AgvNodeAction> actions =
        [
            new() { ActionType = "pick" },
            new() { ActionType = "startCharging" }
        ];

        ICollection<NodeType> result = actions.InferNodeType();

        result.ShouldContain(NodeType.Pick);
        result.ShouldContain(NodeType.Charge);
    }

    [TestMethod]
    public void InferNodeType_NoActions_ReturnsWayPoint()
    {
        ICollection<AgvNodeAction> actions = [];

        ICollection<NodeType> result = actions.InferNodeType();

        result.ShouldHaveSingleItem().ShouldBe(NodeType.WayPoint);
    }

    [TestMethod]
    public void InferNodeType_UnknownActionType_ReturnsWayPoint()
    {
        ICollection<AgvNodeAction> actions = [new() { ActionType = "customAction" }];

        ICollection<NodeType> result = actions.InferNodeType();

        result.ShouldHaveSingleItem().ShouldBe(NodeType.WayPoint);
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

        ICollection<NodeType> result = actions.InferNodeType();

        result.ShouldContain(NodeType.PickDrop);
        result.ShouldContain(NodeType.Charge);
    }
}