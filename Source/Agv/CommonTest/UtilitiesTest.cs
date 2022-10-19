using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
    /// <summary>
    /// Test for utility <see cref="StringExtensions"/>.
    /// </summary>
    [TestClass]
    public class UtilitiesTest
    {
        private const string TestPattern = "#HashMe#";
        private const string HashPattern = "a53178cb-6f56-b8b8-638e-3a2447004b78";

        [TestMethod]
        public void String_ToGuid()
        {
            var expected = new Guid(HashPattern);
            var current = TestPattern.ToGuid();
            Assert.AreEqual(expected, current);
            current = TestPattern.ToGuid();
            Assert.AreEqual(expected, current);
        }

        /// <summary>
        /// Test node comparer by ID
        /// </summary>
        [TestMethod]
        public void NodeIdComparer()
        {
            var comparer = new NodeIdComparer();
            var node1 = new AgvGraph.Node("1") { X = 1f, Y = 2f, Z = 3f, Enabled = true, Type = AgvGraph.NodeType.Charge };
            var node2 = new AgvGraph.Node("2") { X = 1f, Y = 2f, Z = 3f, Enabled = true, Type = AgvGraph.NodeType.Charge };

            Assert.IsFalse(comparer.Equals(node1, node2));
            Assert.AreNotEqual(comparer.GetHashCode(node1), comparer.GetHashCode(node2));
            node2.Id = "1";
            Assert.IsTrue(comparer.Equals(node1, node2));
            Assert.AreEqual(comparer.GetHashCode(node1), comparer.GetHashCode(node2));
            node2.X = 0f;
            node2.Y = 0f;
            node2.Z = 0f;
            node2.Enabled = false;
            node2.Type = AgvGraph.NodeType.Park;
            Assert.IsTrue(comparer.Equals(node1, node2));
            Assert.AreEqual(comparer.GetHashCode(node1), comparer.GetHashCode(node2));
        }

        /// <summary>
        /// Test edge comparer by from/to
        /// </summary>
        [TestMethod]
        public void EdgeIdComparer()
        {
            var comparer = new EdgeIdComparer();
            var edge1 = new AgvGraph.Edge("1", "2") { Metric = 1f };
            var edge2 = new AgvGraph.Edge("2", "1") { Metric = 1f };

            Assert.IsFalse(comparer.Equals(edge1, edge2));
            Assert.AreNotEqual(comparer.GetHashCode(edge1), comparer.GetHashCode(edge2));
            edge2.From = "1";
            edge2.To = "2";
            Assert.IsTrue(comparer.Equals(edge1, edge2));
            Assert.AreEqual(comparer.GetHashCode(edge1), comparer.GetHashCode(edge2));
            edge2.Metric = 2f;
            Assert.IsTrue(comparer.Equals(edge1, edge2));
            Assert.AreEqual(comparer.GetHashCode(edge1), comparer.GetHashCode(edge2));
        }
    }
}
