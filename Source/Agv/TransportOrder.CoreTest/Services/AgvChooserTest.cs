using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
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
            return new IGraphAlgorithm.Route(
                costs: _random.NextSingle(),
                nodes: new[] { source, destination }
            );
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
            var route = CalculateRoute(source, destination);
            return new IGraphAlgorithm.Route(
                costs: 2f * route.Costs,
                nodes: new[] { currentNode }.Concat(route.Nodes).ToArray()
            );
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
            return new IGraphAlgorithm.Route(0f, new[] { source, destination });
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
            return new IGraphAlgorithm.Route(0f, new[] { currentNode, source, destination });
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
        public FailingAlgorithm(AgvGraph graph, IRoutingPlausibilityChecker routingPlausibilityChecker)
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
            return new IGraphAlgorithm.Route(0f, new[] { source, destination });
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
            return new IGraphAlgorithm.Route(0f, new[] { currentNode, source, destination });
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
        private readonly OrderDetails? _dummyOrder;
        private readonly Mock<IRoutingPlausibilityChecker> _routingPlausibilityCheckerMock;
        private IRoutingPlausibilityChecker RoutingPlausibilityChecker => _routingPlausibilityCheckerMock.Object;

        /// <summary>
        /// Test initialization.
        /// </summary>
        public AgvChooserTest()
        {
            var nodes = new[]
                {
                    new AgvGraph.Node("Saarbrücken") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node("Kaiserslautern") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 42f, Y = 80f, Z = 0f },
                    new AgvGraph.Node("Frankfurt") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = 100f, Z = 0f },
                    new AgvGraph.Node("Ludwigshafen") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 96f, Y = 60f, Z = 0f },
                    new AgvGraph.Node("Karlsruhe") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = -10f, Z = 0f },
                    new AgvGraph.Node("Heilbronn") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 150f, Y = 20f, Z = 0f },
                    new AgvGraph.Node("Würzburg") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 200f, Y = 92f, Z = 0f },
                    // We extend the graph with a non reachable node.
                    new AgvGraph.Node("NoWhere") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 210f, Y = -50f, Z = 0f }
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

            const string relatedMapName = "Area51";

            _graphWikipedia = new AgvGraph(Guid.Empty, nodes, edges, relatedMapName.ToGuid(), relatedMapName);

            AgvGraph.Edge[] edgesFast = edges.Select(e => (AgvGraph.Edge)e.Clone()).ToArray();
            foreach (var edge in edgesFast)
            {
                edge.Metric /= 2;
            }

            _graphWikipediaFast = new AgvGraph(Guid.Empty, nodes, edgesFast, relatedMapName.ToGuid(), relatedMapName);

            _dummyOrder = new OrderDetails(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "dummy",
                destination: "dummy",
                leNo: "dummy",
                creator: HostOrderCreator.Simulation
            );

            _routingPlausibilityCheckerMock = new Mock<IRoutingPlausibilityChecker>();
            _routingPlausibilityCheckerMock.Setup(c => c.IsPlausible(It.IsAny<AgvGraph.Node>(), It.IsAny<AgvGraph.Node>()))
                .Returns(true);
        }


        /// <summary>
        /// Call chooser with empty AGV listing returns null
        /// </summary>
        [TestMethod]
        public void CallChooser_WithEmptyEnumeration_Returns_Null()
        {
            var chooser = new AgvChooser<RandomGraphAlgorithm>(RoutingPlausibilityChecker);
            Assert.IsNull(chooser.ChooseAgv(Array.Empty<Messages.Agv>(), _dummyOrder!));
        }

        /// <summary>
        /// Call chooser with missing graph information, expect exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CallChooser_WithoutGraph_ExpectException()
        {
            var chooser = new AgvChooser<AStar>(RoutingPlausibilityChecker); // Use AStar because it depends on graphs.
            var agvData = new List<Messages.Agv>()
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
            var chooser = new AgvChooser<InvalidAlgorithm>(RoutingPlausibilityChecker);
            var agvData = new List<Messages.Agv>()
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
            var chooser = new AgvChooser<FailingAlgorithm>(RoutingPlausibilityChecker);
            var agvData = new List<Messages.Agv>()
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
            var chooser = new AgvChooser<AStar>(RoutingPlausibilityChecker);
            var agvData = new List<Messages.Agv>()
            {
                new() { AgvGraph = _graphWikipedia }
            };

            var order = new OrderDetails(
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

            order = new OrderDetails(
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
            var chooser = new AgvChooser<AStar>(RoutingPlausibilityChecker);
            var agvData = new List<Messages.Agv>()
            {
                new() { AgvGraph = _graphWikipedia }
            };
            var order = new OrderDetails(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "Saarbrücken",
                destination: "Würzburg",
                leNo: "don't care",
                creator: HostOrderCreator.Simulation
            );
            KeyValuePair<Guid, IGraphAlgorithm.Route>? chosenAgv = chooser.ChooseAgv(agvData, order);
            Assert.IsNotNull(chosenAgv);
            Assert.AreEqual(agvData[0].CorrelationId, chosenAgv.Value.Key);
        }

        /// <summary>
        /// Test with a single graph.
        /// </summary>
        [TestMethod]
        public void CallChooser_NonReachableNode_NoPath()
        {
            var chooser = new AgvChooser<AStar>(RoutingPlausibilityChecker);
            var agvData = new List<Messages.Agv>()
            {
                new() { AgvGraph = _graphWikipedia }
            };
            var order = new OrderDetails(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "Kaiserslautern",
                destination: "NoWhere",
                leNo: "don't care",
                creator: HostOrderCreator.Simulation
            );
            KeyValuePair<Guid, IGraphAlgorithm.Route>? chosenAgv = chooser.ChooseAgv(agvData, order);
            Assert.IsFalse(chosenAgv.HasValue);
        }

        /// <summary>
        /// Test with a single graph.
        /// </summary>
        [TestMethod]
        public void CallChooser_DifferentGraphs_UseFastest()
        {
            var chooser = new AgvChooser<AStar>(RoutingPlausibilityChecker);

            var agvData = new List<Messages.Agv>
            {
                new() { AgvId = "Std1", AgvGraph = _graphWikipedia },
                new() { AgvId = "Std2", AgvGraph = _graphWikipedia },
                new() { AgvId = "Fast", AgvGraph = _graphWikipediaFast },
                new() { AgvId = "Std3", AgvGraph = _graphWikipedia }
            };

            var order = new OrderDetails(
                correlationId: Guid.NewGuid(),
                orderId: "dummy",
                source: "Kaiserslautern",
                destination: "Heilbronn",
                leNo: "don't care",
                creator: HostOrderCreator.Simulation
            );
            KeyValuePair<Guid, IGraphAlgorithm.Route>? chosenAgv = chooser.ChooseAgv(agvData, order);
            Assert.IsNotNull(chosenAgv);
            var agv = agvData.Single(x => x.CorrelationId == chosenAgv.Value.Key);
            Assert.AreEqual("Fast", agv.AgvId);
        }
    }
}
