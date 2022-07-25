using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation
{

    /// <summary>
    /// Grid Generator Test
    /// </summary>
    [TestClass]
    public class GridGeneratorTest
    {
    
        /// <summary>
        /// Generate Grid
        /// </summary>
        [TestMethod]
        [DataRow(0, 0, 0)]
        [DataRow(0, 0, 1)]
        [DataRow(0, 1, 1)]
        [DataRow(1, 0, 1)]
        [DataRow(1, 1, 1)]
        [DataRow(3, 1, 1)]
        [DataRow(1, 3, 1)]
        [DataRow(3, 3, 1)]
        [DataRow(1, 0, 1)]
        [DataRow(2, 2, 2)]
        [DataRow(10, 10, 10)]
        public void GenerateGrid_TestGridOfDifferentSizes_AllSuccessful(
            int numberOfSourceNodes, 
            int numberOfDestinationNodes,
            int gridLength)
        {
            string[] sources = GenerateNodeIds("S", numberOfSourceNodes);
            string[] destinations = GenerateNodeIds("D", numberOfDestinationNodes);
            var correlationId = Guid.NewGuid();
            
            var generator = new GridGraphGenerator(sources, destinations, gridLength);
            var graph = generator.Generate(correlationId);
            
            int gridWidth = Math.Max(sources.Length, destinations.Length);
            
            Assert.AreEqual(correlationId, graph.CorrelationId);
            Assert.AreEqual(sources.Length + 
                            destinations.Length + 
                            gridWidth * gridLength, 
                graph.Nodes.Length);

            int expectedNumberOfEdges = (
                sources.Length +
                destinations.Length +
                (gridWidth - 1) * gridLength + // horizontal
                gridWidth * (gridLength - 1) // vertical
                ) * 2;

            if (numberOfSourceNodes == 0 && numberOfDestinationNodes == 0)
            {
                expectedNumberOfEdges = 0;
            }
            
            Assert.AreEqual(expectedNumberOfEdges, graph.Edges.Length);
            
            foreach (string s in sources)
            {
                Assert.IsTrue(graph.Nodes.Any(n => n.Id == s));
            }
            foreach (string d in destinations)
            {
                Assert.IsTrue(graph.Nodes.Any(n => n.Id == d));
            }
        }

        /// <summary>
        /// Test adding service nodes
        /// </summary>
        [TestMethod]
        public void GenerateGrid_TwoServiceNode_Successful()
        {
            string[] sources = GenerateNodeIds("S", 2);
            string[] destinations = GenerateNodeIds("D", 2);
            var generator = new GridGraphGenerator(sources, destinations, 1, 
                new [] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}
                );
            var graph = generator.Generate(Guid.NewGuid());
            
            Assert.AreEqual(1, graph.Nodes.Count(n => n.Type is AgvGraph.NodeType.Charge));
            Assert.AreEqual(1,graph.Nodes.Count(n => n.Type is AgvGraph.NodeType.Park));
        }
        
        /// <summary>
        /// Test adding service nodes
        /// </summary>
        [TestMethod]
        public void GenerateGrid_FourServiceNode_Successful()
        {
            var serviceNodes = new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park};
            string[] sources = GenerateNodeIds("S", 2);
            string[] destinations = GenerateNodeIds("D", 2);
            var generator = new GridGraphGenerator(sources, destinations, 2, serviceNodes);
            var graph = generator.Generate(Guid.NewGuid());
            
            Assert.AreEqual(2, graph.Nodes.Count(n => n.Type is AgvGraph.NodeType.Charge));
            Assert.AreEqual(2,graph.Nodes.Count(n => n.Type is AgvGraph.NodeType.Park));
        }
        
        /// <summary>
        /// Test adding service nodes
        /// </summary>
        [TestMethod]
        public void GenerateGrid_FourServiceNode_AllNodeIds_Unique()
        {
            var serviceNodes = new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park};
            string[] sources = GenerateNodeIds("S", 2);
            string[] destinations = GenerateNodeIds("D", 2);
            var generator = new GridGraphGenerator(sources, destinations, 2, serviceNodes);
            var graph = generator.Generate(Guid.NewGuid());
            Assert.AreEqual(new HashSet<string>(graph.Nodes.Select(n => n.Id)).Count, graph.Nodes.Length);
        }
        
        /// <summary>
        /// Test adding service nodes
        /// </summary>
        [TestMethod]
        public void GenerateGrid_FourServiceNode_AllEdges_Unique()
        {
            var serviceNodes = new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park};
            string[] sources = GenerateNodeIds("S", 2);
            string[] destinations = GenerateNodeIds("D", 2);
            var generator = new GridGraphGenerator(sources, destinations, 2, serviceNodes);
            var graph = generator.Generate(Guid.NewGuid());
            Assert.AreEqual(new HashSet<string>(graph.Edges.Select(n => $"{n.From}.{n.To}")).Count, graph.Edges.Length);
        }
        
        /// <summary>
        /// Test if all edges have been added
        /// </summary>
        [TestMethod]
        public void GenerateGrid_TwoServiceNode_AllEdgesAdded()
        {
            var serviceNodes = new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park};
            string[] sources = GenerateNodeIds("S", 2);
            string[] destinations = GenerateNodeIds("D", 2);
            var generator = new GridGraphGenerator(sources, destinations, 3, serviceNodes);
            var graph = generator.Generate(Guid.NewGuid());

            string[][] expectedEdges =
            {
                // Grid
                new [] {"N0000-0000", "N0000-0001"}, 
                new [] {"N0000-0000", "N0001-0000"}, 
                new [] {"N0000-0001", "N0001-0001"}, 
                new [] {"N0001-0000", "N0001-0001"},
                new [] {"N0001-0000", "N0002-0000"}, 
                new [] {"N0001-0001", "N0002-0001"},
                new [] {"N0002-0000", "N0002-0001"},
                // Source
                new [] {"S0", "N0000-0000"},
                new [] {"S1", "N0000-0001"},
                // Destination
                new [] {"D0", "N0002-0000"},
                new [] {"D1", "N0002-0001"},
                // Service
                new [] {"Charge-0", "N0000-0000"},
                new [] {"Park-1", "N0000-0001"}
            };
            
            
            string[] graphEdges = graph.Edges.Select(e => $"{e.From}-{e.To}").ToArray();

            // Check for edges including bidirectional
            foreach (string[] edge in expectedEdges.Concat(expectedEdges.Reverse()))
            {
                string edgeName = $"{edge[0]}-{edge[1]}";
                Assert.AreEqual(edgeName, graphEdges.FirstOrDefault(n => n == edgeName));
            }
            
            // 2*Expect for bidirectional
            Assert.AreEqual(expectedEdges.Length * 2, graph.Edges.Length);
        }
        
        /// <summary>
        /// Test adding service nodes
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void GenerateGrid_WithThreeServiceNode_GridLengthOne_ExpectIndexOutOfRangeException()
        {
            string[] sources = GenerateNodeIds("S", 2);
            string[] destinations = GenerateNodeIds("D", 2);
            var generator = new GridGraphGenerator(sources, destinations, 1, 
                new [] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Charge}
                );
            generator.Generate(Guid.NewGuid());
        }

        private static string[] GenerateNodeIds(string suffix, int count)
        {
            return Enumerable.Range(0, count).Select(idx => $"{suffix}{idx}").ToArray();
        }
    }
}
