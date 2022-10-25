using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Helpers;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Helpers
{
    [TestClass]
    public class NodeTyperHelperTest
    {
        [DataTestMethod]
        [DataRow(AgvGraph.NodeType.Pick)]
        [DataRow(AgvGraph.NodeType.Drop)]
        [DataRow(AgvGraph.NodeType.PickDrop)]
        public void IsActionNodeType_ReturnsTrue_WhenNodeTypeIsAction(AgvGraph.NodeType nodeType)
        {
            bool isSource = NodeTypeHelper.IsActionNodeType(nodeType);
            isSource.ShouldBeTrue();
        }

        [DataTestMethod]
        [DataRow(AgvGraph.NodeType.WayPoint)]
        [DataRow(AgvGraph.NodeType.Charge)]
        [DataRow(AgvGraph.NodeType.Park)]
        public void IsActionNodeType_ReturnsFalse_WhenNodeTypeIsNoAction(AgvGraph.NodeType nodeType)
        {
            bool isSource = NodeTypeHelper.IsActionNodeType(nodeType);
            isSource.ShouldBeFalse();
        }

        [DataTestMethod]
        [DataRow(AgvGraph.NodeType.Pick)]
        [DataRow(AgvGraph.NodeType.PickDrop)]
        public void IsSource_ReturnsTrue_WhenNodeTypeIsSource(AgvGraph.NodeType nodeType)
        {
            bool isSource = NodeTypeHelper.IsSourceNodeType(nodeType);
            isSource.ShouldBeTrue();
        }

        [DataTestMethod]
        [DataRow(AgvGraph.NodeType.WayPoint)]
        [DataRow(AgvGraph.NodeType.Charge)]
        [DataRow(AgvGraph.NodeType.Drop)]
        [DataRow(AgvGraph.NodeType.Park)]
        public void IsSource_ReturnsFalse_WhenNodeTypeIsNoSource(AgvGraph.NodeType nodeType)
        {
            bool isSource = NodeTypeHelper.IsSourceNodeType(nodeType);
            isSource.ShouldBeFalse();
        }

        [DataTestMethod]
        [DataRow(AgvGraph.NodeType.Drop)]
        [DataRow(AgvGraph.NodeType.PickDrop)]
        public void IsDestination_ReturnsTrue_WhenNodeTypeIsDestination(AgvGraph.NodeType nodeType)
        {
            bool isSource = NodeTypeHelper.IsDestinationNodeType(nodeType);
            isSource.ShouldBeTrue();
        }

        [DataTestMethod]
        [DataRow(AgvGraph.NodeType.WayPoint)]
        [DataRow(AgvGraph.NodeType.Charge)]
        [DataRow(AgvGraph.NodeType.Pick)]
        [DataRow(AgvGraph.NodeType.Park)]
        public void IsDestination_ReturnsFalse_WhenNodeTypeIsNoDestination(AgvGraph.NodeType nodeType)
        {
            bool isSource = NodeTypeHelper.IsDestinationNodeType(nodeType);
            isSource.ShouldBeFalse();
        }
    }
}
