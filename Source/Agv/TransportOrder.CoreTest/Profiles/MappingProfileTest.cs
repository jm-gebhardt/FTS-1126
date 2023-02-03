using System.Collections.Immutable;
using System.Text;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles
{
    /// <summary>
    /// Tests for <see cref="MappingProfile"/>
    /// </summary>
    [TestClass]
    public class MappingProfileTest
    {
        private const double FloatComparisonThreshold = 0.0001;

        private Mapper GetMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });
            var mapper = new Mapper(mapperConfig);
            return mapper;
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void AssertConfigurationIsValid_DoesNotThrowException_WhenMappingProfileIsDefined()
        {
            var mapper = GetMapper();
            mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }

        /// <summary>
        /// Maps the <see cref="AgvGraph"/> to <see cref="GraphDto"/> and should map correctly
        /// </summary>
        [TestMethod]
        public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvGraphIsMappedToGraph()
        {
            const string mapName = "Map#1";
            var agvGraph = new AgvGraph(Guid.NewGuid(),
                new[]
                {
                    new AgvGraph.Node("S1") { Enabled = true, Type = AgvGraph.NodeType.Pick, X =  0f, Y = -2f, Z = 0f },
                    new AgvGraph.Node("D1") { Enabled = true, Type = AgvGraph.NodeType.Drop, X =  4f, Y =  6f, Z = 0f },
                    new AgvGraph.Node("S2") { Enabled = true, Type = AgvGraph.NodeType.Pick, X =  7f, Y =  4f, Z = 0f },
                    new AgvGraph.Node("D2") { Enabled = true, Type = AgvGraph.NodeType.Drop, X = -3f, Y =  0f, Z = 0f }
                },
                new[]
                {
                   new AgvGraph.Edge("S1", "D1") { Metric = 8.944f },   // Just take the distance
                   new AgvGraph.Edge("S2", "D2") { Metric = 10.77f }    // as metric.
                },
                mapName.ToGuid(),
                mapName);

            var mapper = GetMapper();
            var graph = mapper.Map<GraphDto>(agvGraph);

            graph.ShouldNotBeNull();

            graph.Nodes.ShouldNotBeEmpty();
            graph.Edges.ShouldNotBeEmpty();

            graph.Nodes.Count.ShouldBe(agvGraph.Nodes.Length);
            graph.Edges.Count.ShouldBe(agvGraph.Edges.Length);

            graph.CorrelationId.ShouldBe(agvGraph.CorrelationId);

            graph.Nodes.ShouldAllBe(x => agvGraph.Nodes.Any(n => n.Equals(x) &&
                   n.Id == x.Id &&
                   n.Enabled == x.Enabled &&
                   n.Type == x.Type &&
                   Math.Abs(n.X - x.X) < FloatComparisonThreshold &&
                   Math.Abs(n.Y - x.Y) < FloatComparisonThreshold &&
                   Math.Abs(n.Z - x.Z) < FloatComparisonThreshold));
            graph.Edges.ShouldAllBe(x => agvGraph.Edges.Any(e => e.From == x.From.Id && e.To == x.To.Id));
        }

        /// <summary>
        /// Maps the <see cref="AgvSaga"/> to <see cref="AgvDto"/> and should map correctly
        /// </summary>
        [TestMethod]
        public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvSagaIsMappedToAgv()
        {
            const string agvId = "AGV1";
            var correlationId = agvId.ToGuid();
            const string mapName = "Map#1";
            var agvSaga = new AgvSaga(correlationId, agvId, "TestState",
                new AgvStatus(Guid.NewGuid(), "123", true, true, 10, false, false),
                new AgvGraph(Guid.NewGuid(),
                    new AgvGraph.Node[] { new("id") },
                    new AgvGraph.Edge[] { new("from", "to") },
                    mapName.ToGuid(),
                    mapName)
            );

            var mapper = GetMapper();
            var agv = mapper.Map<AgvDto>(agvSaga);

            agv.ShouldNotBeNull();
            agv.CorrelationId.ShouldBe(agvSaga.CorrelationId);
            agv.AgvId.ShouldBe(agvSaga.AgvId);
            agv.CurrentState.ShouldBe(agvSaga.CurrentState);
        }

        /// <summary>
        /// Maps the <see cref="Core.Persistence.Models.Map"/> to <see cref="MapDto"/> and should map correctly
        /// </summary>
        [TestMethod]
        public void Map_ShouldMapAllPropertiesCorrectly_WherePersistenceMapIsMappedToDtoMap()
        {
            var mapper = GetMapper();
            const string base64String = "dGhlIHF1aWNrIGJyb3duIGZveCBqdW1wIG92ZXIgdGhlIGxhenkgZG9n";
            var correlationId = Guid.NewGuid();

            var persistenceMap = new Core.Persistence.Models.Map(correlationId, "Area51_Floor1_Compartment47", Encoding.UTF8.GetBytes(base64String), 0.01, 1, 2, DateTime.UtcNow);
            ImmutableDictionary<Guid, Guid[]> agvMapRelation = new Dictionary<Guid, Guid[]>
            {
                {
                    correlationId,
                    new[]
                    {
                        new Guid("3D3D5DA8-1A01-459E-B0DA-EBB875D393F6"),
                        new Guid("A781AF33-CB57-4143-8EEF-1590FE9EC2ED")
                    }
                }
            }.ToImmutableDictionary(x => x.Key, x => x.Value);

            var dtoMap = mapper.Map<MapDto>(persistenceMap, opt => opt.Items[MappingProfile.AgvMapRelationsName] = agvMapRelation);

            dtoMap.ShouldNotBeNull();
            dtoMap.CorrelationId.ShouldBe(persistenceMap.CorrelationId);
            dtoMap.MapId.ShouldBe(persistenceMap.MapId);
            dtoMap.Resolution.ShouldBe(persistenceMap.Resolution);
            dtoMap.Resolution.ShouldBe(persistenceMap.Resolution);
            dtoMap.XOffset.ShouldBe(persistenceMap.XOffset);
            dtoMap.YOffset.ShouldBe(persistenceMap.YOffset);
            dtoMap.RelatedAgvs.ShouldAllBe(p => agvMapRelation.Any(kvp => kvp.Key.Equals(dtoMap.CorrelationId) && kvp.Value.Contains(p)));
        }

        /// <summary>
        /// Maps the <see cref="TransportOrder.Core.States.OrderSaga"/> to <see cref="OrderDto"/> and should map correctly
        /// </summary>
        [TestMethod]
        public void Map_ShouldMapAllPropertiesCorrectly_WhereOrderSagaIsMappedToOrder()
        {
            string orderId = "Order123";
            var orderSaga = new OrderSaga(
                orderId.ToGuid(),
                new OrderDetails(orderId.ToGuid(), orderId, "S1", "D1", "1234", HostOrderCreator.Simulation),
                                 nameof(Core.Services.OrderStateMachine.TransportOrderSent),
                                 null
            );

            var mapper = GetMapper();
            var order = mapper.Map<OrderDto>(orderSaga);

            order.ShouldNotBeNull();
            order.CurrentState.ShouldBe(orderSaga.CurrentState);
            order.AssignedAgv.ShouldBe(orderSaga.AssignedAgv);
            order.CorrelationId.ShouldBe(orderSaga.CorrelationId);
            order.FailedReason.ShouldBe(orderSaga.FailedReason);

            order.OrderDetails.ShouldSatisfyAllConditions(p => p.CorrelationId.ShouldBe(orderSaga.OrderDetails.CorrelationId),
                p => p.OrderId.ShouldBe(orderSaga.OrderDetails.OrderId),
                p => p.Source.ShouldBe(orderSaga.OrderDetails.Source),
                p => p.Destination.ShouldBe(orderSaga.OrderDetails.Destination),
                p => p.LeNo.ShouldBe(orderSaga.OrderDetails.LeNo),
                p => p.Creator.ShouldBe(orderSaga.OrderDetails.Creator),
                p => p.CreationDate.ShouldBe(orderSaga.OrderDetails.CreationDate));
        }

        /// <summary>
        /// Maps the <see cref="AgvPositionChanged"/> to <see cref="AgvPositionStatus"/> and should map correctly
        /// </summary>
        [TestMethod]
        public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvPositionChangedIsMappedToAgvPositionStatus()
        {
            string agvId = "agv1";
            var agvPositionChanged = new AgvPositionChanged(
                new AgvPosition(false, 0, 0, 0, 0),
                null,
                agvId,
                agvId.ToGuid()
            );

            var mapper = GetMapper();
            var agvPositionStatus = mapper.Map<AgvPositionStatus>(agvPositionChanged);

            agvPositionStatus.ShouldNotBeNull();
            agvPositionStatus.AgvPosition.ShouldSatisfyAllConditions(s => s.PositionInitialized.ShouldBe(agvPositionChanged.NewAgvPosition.PositionInitialized),
                s => s.X.ShouldBe(agvPositionChanged.NewAgvPosition.X),
                s => s.Y.ShouldBe(agvPositionChanged.NewAgvPosition.Y),
                s => s.Z.ShouldBe(agvPositionChanged.NewAgvPosition.Z),
                s => s.Theta.ShouldBe(agvPositionChanged.NewAgvPosition.Theta),
                s => s.DeviationRange.ShouldBe(agvPositionChanged.NewAgvPosition.DeviationRange));

            agvPositionStatus.AgvId.ShouldBe(agvPositionChanged.AgvId);
            agvPositionStatus.CorrelationId.ShouldBe(agvPositionChanged.CorrelationId);
        }

        /// <summary>
        /// Maps the <see cref="AgvPositionReceived"/> to <see cref="AgvPositionStatus"/> and should map correctly
        /// </summary>
        [TestMethod]
        public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvPositionReceivedIsMappedToAgvPositionStatus()
        {
            string agvId = "agv1";
            var agvPositionReceived = new AgvPositionReceived(
                agvId.ToGuid(),
                agvId,
                new AgvPosition(false, 0, 0, 0, 0)
            );

            var mapper = GetMapper();
            var agvPositionStatus = mapper.Map<AgvPositionStatus>(agvPositionReceived);

            agvPositionStatus.ShouldNotBeNull();
            agvPositionStatus.AgvPosition.ShouldSatisfyAllConditions(s => s.PositionInitialized.ShouldBe(agvPositionReceived.AgvPosition.PositionInitialized),
                s => s.X.ShouldBe(agvPositionReceived.AgvPosition.X),
                s => s.Y.ShouldBe(agvPositionReceived.AgvPosition.Y),
                s => s.Z.ShouldBe(agvPositionReceived.AgvPosition.Z),
                s => s.Theta.ShouldBe(agvPositionReceived.AgvPosition.Theta),
                s => s.DeviationRange.ShouldBe(agvPositionReceived.AgvPosition.DeviationRange));

            agvPositionStatus.AgvId.ShouldBe(agvPositionReceived.AgvId);
            agvPositionStatus.CorrelationId.ShouldBe(agvPositionReceived.CorrelationId);
        }
    }
}
