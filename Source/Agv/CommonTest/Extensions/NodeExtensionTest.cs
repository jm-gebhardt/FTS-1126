using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Extensions;

[TestClass]
public class NodeExtensionTest
{
    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.PickDrop)]
    public void IsPickStation_PickTypes_True(AgvGraph.NodeType nodeType)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).IsPickStation().ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop })]
    [DataRow(new[] { AgvGraph.NodeType.Park, AgvGraph.NodeType.Pick })]
    public void IsPickStation_PickTypesAdditionalType_True(ICollection<AgvGraph.NodeType> nodeTypes)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).IsPickStation().ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Drop)]
    [DataRow(AgvGraph.NodeType.Charge)]
    [DataRow(AgvGraph.NodeType.WayPoint)]
    public void IsPickStation_NonPickTypes_False(AgvGraph.NodeType nodeType)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).IsPickStation().ShouldBeFalse();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Drop)]
    [DataRow(AgvGraph.NodeType.PickDrop)]
    public void IsDropStation_DropTypes_True(AgvGraph.NodeType nodeType)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).IsDropStation().ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop })]
    [DataRow(new[] { AgvGraph.NodeType.Drop, AgvGraph.NodeType.Pick })]
    public void IsDropStation_DropTypesAdditionalType_True(ICollection<AgvGraph.NodeType> nodeTypes)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).IsDropStation().ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.Charge)]
    [DataRow(AgvGraph.NodeType.WayPoint)]
    public void IsDropStation_NonDropTypes_False(AgvGraph.NodeType nodeType)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).IsDropStation().ShouldBeFalse();
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop })]
    [DataRow(new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.WayPoint })]
    [DataRow(new[] { AgvGraph.NodeType.WayPoint, AgvGraph.NodeType.Pick })]
    [DataRow(new[] { AgvGraph.NodeType.WayPoint, AgvGraph.NodeType.Park })]
    [DataRow(new[] { AgvGraph.NodeType.WayPoint, AgvGraph.NodeType.Maintenance })]
    public void IsStation_AdditionalType_True(ICollection<AgvGraph.NodeType> nodeTypes)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).IsStation().ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Drop)]
    [DataRow(AgvGraph.NodeType.PickDrop)]
    [DataRow(AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.Park)]
    [DataRow(AgvGraph.NodeType.Charge)]
    [DataRow(AgvGraph.NodeType.Maintenance)]
    public void IsStation_ValidData_True(AgvGraph.NodeType nodeType)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).IsStation().ShouldBeTrue();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.WayPoint)]
    public void IsStation_Waypoint_False(AgvGraph.NodeType nodeType)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).IsStation().ShouldBeFalse();
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop }, false)]
    [DataRow(new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.WayPoint }, true)]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Charge }, true)]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Charge }, true)]
    public void NeedsReservation_AdditionalType_ExpectedValue(ICollection<AgvGraph.NodeType> nodeTypes, bool expected)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).NeedsReservation().ShouldBe(expected);
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop }, false)]
    [DataRow(new[] { AgvGraph.NodeType.Park, AgvGraph.NodeType.WayPoint }, true)]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Park }, true)]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Charge }, false)]
    public void IsParker_AdditionalType_ExpectedValue(ICollection<AgvGraph.NodeType> nodeTypes, bool expected)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).IsParker().ShouldBe(expected);
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop }, false)]
    [DataRow(new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.WayPoint }, true)]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Charge }, true)]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park }, false)]
    public void IsCharger_AdditionalType_ExpectedValue(ICollection<AgvGraph.NodeType> nodeTypes, bool expected)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).IsCharger().ShouldBe(expected);
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop }, AgvGraph.NodeType.Pick)]
    [DataRow(new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.WayPoint }, AgvGraph.NodeType.Charge)]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park }, AgvGraph.NodeType.PickDrop)]
    public void GetPrimaryType_AdditionalType_ExpectedValue(ICollection<AgvGraph.NodeType> nodeTypes, AgvGraph.NodeType expected)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).GetPrimaryType().ShouldBe(expected);
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Pick, AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.Charge, AgvGraph.NodeType.Charge)]
    [DataRow(AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.PickDrop)]
    public void GetPrimaryType_AdditionalType_ExpectedValue(AgvGraph.NodeType nodeType, AgvGraph.NodeType expected)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).GetPrimaryType().ShouldBe(expected);
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop }, new[] { AgvGraph.NodeType.Drop })]
    [DataRow(new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.WayPoint }, new[] { AgvGraph.NodeType.WayPoint })]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park, AgvGraph.NodeType.Maintenance }, new[] { AgvGraph.NodeType.Park, AgvGraph.NodeType.Maintenance })]
    public void GetAdditionalTypes_AdditionalType_ExpectedValue(ICollection<AgvGraph.NodeType> nodeTypes, ICollection<AgvGraph.NodeType> expected)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).GetAdditionalTypes().ShouldBe(expected);
    }

    [TestMethod]
    public void GetAdditionalTypes_NoAdditionalType_Empty()
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: AgvGraph.NodeType.Charge).GetAdditionalTypes().ShouldBeEmpty();
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.WayPoint }, new[] { AgvGraph.NodeType.Charge })]
    [DataRow(new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }, new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park })]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.Park, AgvGraph.NodeType.Maintenance }, new[] { AgvGraph.NodeType.Park })]
    public void GetReservableNodeTypes_AdditionalType_ValidData_ReturnsReservableNodeTypes(ICollection<AgvGraph.NodeType> nodeTypes, ICollection<AgvGraph.NodeType> expected)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).GetReservableNodeTypes().ShouldBe(expected);
    }

    [DataTestMethod]
    [DataRow(new[] { AgvGraph.NodeType.PickDrop, AgvGraph.NodeType.WayPoint })]
    [DataRow(new[] { AgvGraph.NodeType.Pick, AgvGraph.NodeType.Drop })]
    public void GetReservableNodeTypes_AdditionalType_NoReservableTypes_Empty(ICollection<AgvGraph.NodeType> nodeTypes)
    {
        NodeGenerator.GenerateNodeWithMultipleTypes(nodeId: "id", nodeTypes: nodeTypes.ToList()).GetReservableNodeTypes().ShouldBeEmpty();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Drop)]
    [DataRow(AgvGraph.NodeType.PickDrop)]
    [DataRow(AgvGraph.NodeType.Pick)]
    [DataRow(AgvGraph.NodeType.WayPoint)]
    [DataRow(AgvGraph.NodeType.Maintenance)]
    public void GetReservableNodeTypes_AdditionalType_NoReservableTypes_Empty(AgvGraph.NodeType nodeType)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).GetReservableNodeTypes().ShouldBeEmpty();
    }

    [DataTestMethod]
    [DataRow(AgvGraph.NodeType.Charge)]
    [DataRow(AgvGraph.NodeType.Park)]
    public void GetReservableNodeTypes_AdditionalType_ReservableTypes_ReturnsNodeTypes(AgvGraph.NodeType nodeType)
    {
        NodeGenerator.GenerateNode(nodeId: "id", nodeType: nodeType).GetReservableNodeTypes().ShouldBe([nodeType]);
    }
}