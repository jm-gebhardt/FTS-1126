using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Extensions;

[TestClass]
public class NodeTypeExtensionTest
{
    [TestMethod]
    public void GetTypeForActionType_NotNodeType_ThrowsException()
    {
        const string notNodeTypeObject = "this is not a node type";

        Should.Throw<InvalidCastException>(() => notNodeTypeObject.GetTypeForActionType(ActionType.Charge));
    }

    [DataRow(ActionType.Charge)]
    [DataRow(ActionType.Pick)]
    [TestMethod]
    public void GetTypeForActionType_SingleType_ReturnType(ActionType actionType)
    {
        AgvGraph.NodeType.Charge.GetTypeForActionType(actionType).ShouldBe(AgvGraph.NodeType.Charge);
    }

    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge }, ActionType.Charge, AgvGraph.NodeType.Charge)]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge }, ActionType.Pick, AgvGraph.NodeType.Pick)]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge }, ActionType.Pick, AgvGraph.NodeType.PickDrop)]
    [DataRow(new[] { AgvGraph.NodeType.Drop, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge }, ActionType.Drop, AgvGraph.NodeType.Drop)]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge }, ActionType.Drop, AgvGraph.NodeType.PickDrop)]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge }, ActionType.Move, AgvGraph.NodeType.PickDrop)]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park }, ActionType.Charge, null)]
    [TestMethod]
    public void GetTypeForActionType_AdditionalType_ReturnType(ICollection<AgvGraph.NodeType> nodeTypes, ActionType actionType, AgvGraph.NodeType expected)
    {
        nodeTypes.GetTypeForActionType(actionType).ShouldBe(expected);
    }

    [DataRow(AgvGraph.NodeType.Pick, AgvGraph.NodeType.Charge, false)]
    [DataRow(AgvGraph.NodeType.Pick, AgvGraph.NodeType.Pick, true)]
    [TestMethod]
    public void IsOfType_SingleType_ReturnsExpectedValue(AgvGraph.NodeType nodeType, AgvGraph.NodeType typeToTest, bool expected)
    {
        nodeType.IsOfType(typeToTest).ShouldBe(expected);
    }

    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park }, AgvGraph.NodeType.Charge, false)]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Park }, AgvGraph.NodeType.Pick, true)]
    [TestMethod]
    public void IsOfType_AdditionalType_ReturnsExpectedValue(ICollection<AgvGraph.NodeType> nodeTypes, AgvGraph.NodeType typeToTest, bool expected)
    {
        nodeTypes.IsOfType(typeToTest).ShouldBe(expected);
    }
}