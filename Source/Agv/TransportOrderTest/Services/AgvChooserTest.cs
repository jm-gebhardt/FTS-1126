using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Implementation of interface <see cref="IAgv"/> for testing.
    /// </summary>
    internal class Agv : IAgv
    {
        /// <summary/>
        public Guid CorrelationId { get; } = Guid.NewGuid();
        /// <summary/>
        public string AgvId { get; init; } = "";
        /// <summary/>
        public string CurrentState { get; } = "";
        /// <summary/>
        [JetBrains.Annotations.UsedImplicitly]
        public AgvStatus? AgvStatus { get; set; }
        /// <summary/>
        public AgvGraph? AgvGraph { get; init; }
        /// <summary/>
        public AgvData? AgvData { get; init; }
    }


    /// <summary>
    /// More or less senseless implementation of a graph algorithm
    /// to test dependency injection and generic algorithm templates.
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public class RandomGraphAlgorithm : IGraphAlgorithm
    {
        private readonly Random _random;

        /// <summary>
        /// This algorithm is created using default constructor.
        /// </summary>
        public RandomGraphAlgorithm()
        {
            _random = new Random(Guid.NewGuid().GetHashCode());
        }

        /// <summary>
        /// Return a random value for costs.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public IGraphAlgorithm.Route CalculateRoute(string source, string destination)
        {
            return new IGraphAlgorithm.Route()
            {
                Costs = _random.NextSingle()
            };
        }

        /// <summary>
        /// Return a random value for costs (within the double range than <see cref="CalculateRoute(string, string)"/>.
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public IGraphAlgorithm.Route CalculateRoute(string currentNode, string source, string destination)
        {
            return new IGraphAlgorithm.Route()
            {
                Costs = 2f * CalculateRoute(source, destination).Costs
            };
        }
    }

    /// <summary>
    /// Algorithm without fitting constructor.
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public class InvalidAlgorithm : IGraphAlgorithm
    {
        // ReSharper disable All
        /// <summary>
        /// This algorithm has no fitting constructor.
        /// </summary>
        public InvalidAlgorithm(int anyThing)
        {
        }
        // ReSharper restore All

        /// <summary>
        /// Return a don't care value.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public IGraphAlgorithm.Route CalculateRoute(string source, string destination)
        {
            return new IGraphAlgorithm.Route() { Costs = 0f };
        }

        /// <summary>
        /// Return a don't care value.
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public IGraphAlgorithm.Route CalculateRoute(string currentNode, string source, string destination)
        {
            return new IGraphAlgorithm.Route() { Costs = 0f };
        }
    }

    /// <summary>
    /// Algorithm causing exception while creation.
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public class FailingAlgorithm : IGraphAlgorithm
    {
        /// <summary>
        /// This algorithm has no fitting constructor.
        /// </summary>
        public FailingAlgorithm(AgvGraph graph)
        {
            if (graph != null)
            {
                throw new Exception("Any exception in constructor.");
            }
        }

        /// <summary>
        /// Return a don't care value.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public IGraphAlgorithm.Route CalculateRoute(string source, string destination)
        {
            return new IGraphAlgorithm.Route() { Costs = 0f };
        }

        /// <summary>
        /// Return a don't care value.
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public IGraphAlgorithm.Route CalculateRoute(string currentNode, string source, string destination)
        {
            return new IGraphAlgorithm.Route() { Costs = 0f };
        }
    }


    /// <summary>
    /// Tests for <see cref="AgvChooser{TGraphAlgorithm}"/>.
    /// </summary>
    [TestClass]
    public class AgvChooserTest
    {
        private readonly AgvGraph? _graphWikipedia;
        private readonly AgvGraph? _graphWikipediaFast;
        private readonly List<IAgv>? _nullAgvData;
        private readonly Common.Messages.Order? _nullOrder;
        private readonly Common.Messages.Order? _dummyOrder;


        /// <summary>
        /// Test initialization.
        /// </summary>
        public AgvChooserTest()
        {
            var nodes = new[]
                {
                    new AgvGraph.Node("Saarbrücken") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node("Kaiserslautern") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 42f, Y = 80f, Z = 0f },
                    new AgvGraph.Node("Frankfurt") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 104f, Y = 100f, Z = 0f },
                    new AgvGraph.Node("Ludwigshafen") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 96f, Y = 60f, Z = 0f },
                    new AgvGraph.Node("Karlsruhe") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 104f, Y = -10f, Z = 0f },
                    new AgvGraph.Node("Heilbronn") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 150f, Y = 20f, Z = 0f },
                    new AgvGraph.Node("Würzburg") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 200f, Y = 92f, Z = 0f },
                    // We extend the graph with a non reachable node.
                    new AgvGraph.Node("NoWhere") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 210f, Y = -50f, Z = 0f }
                };
            var edges = new[]
            {
                   new AgvGraph.Edge("Saarbrücken", "Kaiserslautern") { Metric = 70f },
                   new AgvGraph.Edge("Kaiserslautern", "Saarbrücken") { Metric = 70f },
                   new AgvGraph.Edge("Saarbrücken", "Karlsruhe") {Metric = 145f },
                   new AgvGraph.Edge("Karlsruhe", "Saarbrücken") { Metric = 145f },
                   new AgvGraph.Edge("Kaiserslautern", "Frankfurt") { Metric = 103f },
                   new AgvGraph.Edge("Frankfurt", "Kaiserslautern") { Metric = 103f },
                   new AgvGraph.Edge("Kaiserslautern", "Ludwigshafen") { Metric = 53f },
                   new AgvGraph.Edge("Ludwigshafen", "Kaiserslautern") { Metric = 53f },
                   new AgvGraph.Edge("Karlsruhe", "Heilbronn") { Metric = 84f },
                   new AgvGraph.Edge("Heilbronn", "Karlsruhe") { Metric = 84f },
                   new AgvGraph.Edge("Frankfurt", "Würzburg") { Metric = 116f },
                   new AgvGraph.Edge("Würzburg", "Frankfurt") { Metric = 116f },
                   new AgvGraph.Edge("Ludwigshafen", "Würzburg") {  Metric = 183f },
                   new AgvGraph.Edge("Würzburg", "Ludwigshafen") { Metric = 183f },
                   new AgvGraph.Edge("Heilbronn", "Würzburg") { Metric = 102f },
                   new AgvGraph.Edge("Würzburg", "Heilbronn") { Metric = 102f },
                   // You can leave "NoWhere" but you can never go there.
                   new AgvGraph.Edge("NoWhere", "Heilbronn") { Metric = 123f }
                };

            _graphWikipedia = new AgvGraph(Guid.Empty, nodes, edges);

            AgvGraph.Edge[] edgesFast = edges.Select(e => (AgvGraph.Edge)e.Clone()).ToArray();
            foreach (var edge in edgesFast)
            {
                edge.Metric /= 2;
            }

            _graphWikipediaFast = new AgvGraph(Guid.Empty, nodes, edgesFast);

            _nullAgvData = null;

            _nullOrder = null;

            _dummyOrder = new Common.Messages.Order(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "dummy",
                destination: "dummy",
                leNo: "dummy",
                creator: HostOrderCreator.Simulation
            );
        }


        /// <summary>
        /// Call chooser with empty AGV listing, expect exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CallChooser_WithNullEnumeration_ExpectException()
        {
            var chooser = new AgvChooser<RandomGraphAlgorithm>();
            chooser.ChooseAgv(_nullAgvData!, _dummyOrder!);
        }
        
        /// <summary>
        /// Call chooser with empty AGV listing, expect exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CallChooser_WithNullOrder_ExpectException()
        {
            var chooser = new AgvChooser<RandomGraphAlgorithm>();
            var agvData = new List<Agv>
            {
                new() { AgvGraph = _graphWikipedia, AgvData = null}
            };
            chooser.ChooseAgv(agvData, _nullOrder!);
        }

        /// <summary>
        /// Call chooser with empty AGV listing, expect exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CallChooser_WithEmptyEnumeration_ExpectException()
        {
            var chooser = new AgvChooser<RandomGraphAlgorithm>();
            // ReSharper disable All
            var agvData = new List<Agv>();
            chooser.ChooseAgv(agvData, _dummyOrder!);
            // ReSharper restore All
        }

        /// <summary>
        /// Call chooser with missing graph information, expect exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CallChooser_WithoutGraph_ExpectException()
        {
            var chooser = new AgvChooser<AStar>(); // Use AStar because it depends on graphs.
            var agvData = new List<Agv>()
            {
                new() { AgvGraph = null }
            };
            chooser.ChooseAgv(agvData, _dummyOrder!);
        }

        /// <summary>
        /// Call chooser with invalid algorithm, expect exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(MissingMethodException))]
        public void CallChooser_WithInvalidAlgorithm_ExpectException()
        {
            var chooser = new AgvChooser<InvalidAlgorithm>();
            var agvData = new List<Agv>()
            {
                new() { AgvGraph = _graphWikipedia }
            };
            chooser.ChooseAgv(agvData, _dummyOrder!);
        }

        /// <summary>
        /// Call chooser with failing algorithm, expect exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(System.Reflection.TargetInvocationException))]
        public void CallChooser_WithFailingAlgorithm_ExpectException()
        {
            var chooser = new AgvChooser<FailingAlgorithm>();
            var agvData = new List<Agv>()
            {
                new() { AgvGraph = _graphWikipedia }
            };
            chooser.ChooseAgv(agvData, _dummyOrder!);
        }

        /// <summary>
        /// Call chooser with missing source/destination, expect exception.
        /// </summary>
        [TestMethod]
        public void CallChooser_WithMissingSourceDestination_ExpectExceptions()
        {
            var chooser = new AgvChooser<AStar>();
            var agvData = new List<Agv>()
            {
                new() { AgvGraph = _graphWikipedia }
            };

            var order = new Common.Messages.Order(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "source",
                destination: "",
                leNo: "don't care",
                creator: HostOrderCreator.Simulation
            );
            // ReSharper disable All
            Assert.ThrowsException<ArgumentException>(() => chooser.ChooseAgv(agvData, order));
            // ReSharper restore All

            order = new Common.Messages.Order(
               correlationId: Guid.NewGuid(),
               orderId: "dummy",
               source: "",
               destination: "destination",
               leNo: "don't care",
               creator: HostOrderCreator.Simulation
            );
            // ReSharper disable All
            Assert.ThrowsException<ArgumentException>(() => chooser.ChooseAgv(agvData, order));
            // ReSharper restore All
        }

        /// <summary>
        /// Test with a single graph.
        /// </summary>
        [TestMethod]
        public void CallChooser_WithValidSingleGraph_Success()
        {
            var chooser = new AgvChooser<AStar>();
            var agvData = new List<Agv>()
            {
                new() { AgvGraph = _graphWikipedia }
            };
            var order = new Common.Messages.Order(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "Saarbrücken",
                destination: "Würzburg",
                leNo: "don't care",
                creator: HostOrderCreator.Simulation
            );
            Guid? agvCorrelation = chooser.ChooseAgv(agvData, order);
            Assert.AreEqual(agvData[0].CorrelationId, agvCorrelation);
        }

        /// <summary>
        /// Test with a single graph.
        /// </summary>
        [TestMethod]
        public void CallChooser_NonReachableNode_NoPath()
        {
            var chooser = new AgvChooser<AStar>();
            var agvData = new List<Agv>()
            {
                new() { AgvGraph = _graphWikipedia }
            };
            var order = new Common.Messages.Order(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "Kaiserslautern",
                destination: "NoWhere",
                leNo: "don't care",
                creator: HostOrderCreator.Simulation
            );
            Guid? agvCorrelation = chooser.ChooseAgv(agvData, order);
            Assert.IsFalse(agvCorrelation.HasValue);
        }

        /// <summary>
        /// Test with a single graph.
        /// </summary>
        [TestMethod]
        public void CallChooser_DifferentGraphs_UseFastest()
        {
            var chooser = new AgvChooser<AStar>();

            var agvData = new List<Agv>
            {
                new() { AgvId = "Std1", AgvGraph = _graphWikipedia },
                new() { AgvId = "Std2", AgvGraph = _graphWikipedia },
                new() { AgvId = "Fast", AgvGraph = _graphWikipediaFast },
                new() { AgvId = "Std3", AgvGraph = _graphWikipedia }
            };

            var order = new Common.Messages.Order(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "Kaiserslautern",
                destination: "Heilbronn",
                leNo: "don't care",
                creator: HostOrderCreator.Simulation
            );
            Guid? agvCorrelation = chooser.ChooseAgv(agvData, order);
            var agv = agvData.Single(x => x.CorrelationId == agvCorrelation);
            Assert.AreEqual("Fast", agv.AgvId);
        }
    }
}
