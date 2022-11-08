using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Routing
{
    [TestClass]
    public class NoCheckPlausibilityCheckerTest
    {
        private IRoutingPlausibilityChecker _routingPlausibilityChecker = null!;

        [TestInitialize]
        public void Setup()
        {
            _routingPlausibilityChecker = new NoCheckPlausibilityChecker();
        }

        [DataTestMethod]
        [DynamicData(nameof(AllNodeToNodes), DynamicDataSourceType.Method)]
        public void IsPlausible_ReturnsTrue_WhereAnyParameter(AgvGraph.Node source, AgvGraph.Node destination)
        {
            bool isPlausible = _routingPlausibilityChecker.IsPlausible(source, destination);
            isPlausible.ShouldBeTrue();
        }

        private static IEnumerable<object[]> AllNodeToNodes()
        {
            foreach (var startNodeType in GetAllNodeTypes())
            {
                foreach (var endNodeType in GetAllNodeTypes())
                {
                    yield return new object[] { CreateNode(startNodeType), CreateNode(endNodeType) };
                }
            }
        }

        private static AgvGraph.NodeType[] GetAllNodeTypes()
        {
            return Enum.GetValues<AgvGraph.NodeType>();
        }

        private static AgvGraph.Node CreateNode(AgvGraph.NodeType nodeType)
        {
            return new AgvGraph.Node(Guid.NewGuid().ToString())
            {
                Type = nodeType
            };
        }
    }
}
