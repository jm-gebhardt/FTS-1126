using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services;

public static class AgvRegistryMock
{
    public static readonly string[] Orders = { "ORDER001", "ORDER002", "ORDER003" };
    public static readonly string[] Sources = { "SRC1", "SRC2", "SRC3" };
    public static readonly string[] Destinations = { "DST1", "DST2", "DST3" };
    private static readonly string[] _agvIds = { "AGV001", "AGV002" };
    private static readonly string[] _agvStates = { "Idle", "Busy" };
    private static readonly string[] _ips = { "127.0.0.1", "127.0.0.2" };

    public static Mock<IAgvRegistry> GetAgvRegistry()
    {
        var registry = new Mock<IAgvRegistry>();

        var agvs = new List<IAgv>();
        const string mapName = "Map#1";

        // Arrange
        string node01 = Sources[0];
        string node02 = Sources[1];
        string node03 = Destinations[0];
        string node04 = Destinations[1];
        string node05 = Sources[2];
        string node06 = Destinations[2];
        var agvGraph = new AgvGraph(Guid.Empty,
            new[]
            {
                new AgvGraph.Node(node01){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f},
                new AgvGraph.Node(node02){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f},
                new AgvGraph.Node(node03){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 3f, Z = 0f},
                new AgvGraph.Node(node04){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 3f, Z = 0f},
                new AgvGraph.Node(node05){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 9f, Z = 0f},
                new AgvGraph.Node(node06){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 13f, Y = 5f, Z = 0f}
            }, new[]
            {
                new AgvGraph.Edge(node01, node02){ Metric = 4f},
                new AgvGraph.Edge(node01, node03){ Metric = 3f},
                new AgvGraph.Edge(node01, node04){ Metric = 5f},
                new AgvGraph.Edge(node02, node01){ Metric = 4f},
                new AgvGraph.Edge(node02, node04){ Metric = 6f},
                new AgvGraph.Edge(node03, node01){ Metric = 3f},
                new AgvGraph.Edge(node03, node04){ Metric = 4f},
                new AgvGraph.Edge(node04, node01){ Metric = 5f},
                new AgvGraph.Edge(node04, node02){ Metric = 6f},
                new AgvGraph.Edge(node04, node03){ Metric = 4f}
                //no edges for node 5 and 6 on purpose so that there is no route to this stations
            },
            mapName.ToGuid(),
            mapName);

        for (int i = 0; i < _agvIds.Length; i++)
        {
            string agvId = _agvIds[i];
            string order = Orders[i];
            string state = _agvStates[i];
            string ip = _ips[i];
            var correlationId = agvId.ToGuid();

            var agvStatus = new AgvStatus(correlationId, order, true, true, 10, true, true);
            var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());

            agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph, agvData) { Alive = true });
        }

        registry.SetupGet(x => x.Count).Returns(agvs.Count);
        registry.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
        registry.Setup(x => x.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
        registry.Setup(x => x.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(agvs));
        registry.Setup(x => x.GetAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId =>
        {
            foreach (var agv in agvs)
            {
                if (agv.CorrelationId == correlationId)
                {
                    return agv;
                }
            }
            return null;
        });

        return registry;
    }
}