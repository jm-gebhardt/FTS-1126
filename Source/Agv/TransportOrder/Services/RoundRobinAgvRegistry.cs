using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.States;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// A simple registry to save state of assigned AGV Id
    /// </summary>
    public class RoundRobinAgvRegistry : IAgvRegistry
    {
        private const string AgvNamePrefix = "AGV";
        private readonly List<IAgv> _agvs;
        private int _current;
        private readonly object _lockObject = new();

        /// <inheritdoc/>
        public int Count => _agvs.Count;

        /// <inheritdoc/>
        public int CountIdle => _agvs.Count;

        /// <summary>
        /// A simple registry to save state of assigned AGV Id.
        /// </summary>
        /// <param name="options">The simulation options that hold the amount of available AGVs</param>
        public RoundRobinAgvRegistry(SimulateOptions options)
        {
            int agvCount = options.Agvs;

            _agvs = new List<IAgv>(agvCount);
            _current = 0;

            var nodes = new AgvGraph.Node[] {
                new("node00") { X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
                new("node10") { X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
                new("node01") { X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint },
                new("node11") { X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint }
            };

            var edges = new AgvGraph.Edge[] {
                new("node00", "node01"),
                new("node01", "node11"),
                new("node11", "node10"),
                new("node10", "node00")
            };

            for (int i = 0; i < agvCount; i++)
            {
                string agvId = $"{AgvNamePrefix}_{i:D3}";
                var correlationId = agvId.ToGuid();

                var agvGraph = new AgvGraph(correlationId, nodes, edges);
                var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);
                var agvData = new AgvData(correlationId, "Karis", "BF1", "127.0.0.1", 1.8, 120.0, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());

                _agvs.Add(new AgvSaga(correlationId, agvId, "Idle", agvStatus, agvGraph, agvData));
            }
        }

        /// <summary>
        /// Get next agv Id thread safe
        /// </summary>
        public string Next()
        {
            lock (_lockObject)
            {
                int next = _current;
                _current = (_current + 1) % _agvs.Count;
                return _agvs[next].AgvId;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IAgv> GetAllAgvs()
        {
            return _agvs;
        }

        /// <inheritdoc/>
        public IEnumerable<IAgv> GetIdleAgvs()
        {
            return _agvs;
        }

        /// <inheritdoc/>
        public IAgv? GetAgv(Guid correlationId)
        {
            foreach (var agv in _agvs)
            {
                if (agv.CorrelationId == correlationId)
                {
                    return agv;
                }
            }

            return null;
        }
    }
}
