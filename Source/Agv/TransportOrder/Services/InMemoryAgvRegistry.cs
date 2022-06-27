using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using System.Collections.Concurrent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Assigns the next free AGV as a response to IAssignAGV
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class InMemoryAgvRegistry : IAgvRegistry
    {
        // This container should be persisted with EntityFramework in a future implementation
        private readonly ConcurrentDictionary<Guid, Common.Messages.AgvManagement.Agv> _agvs;
        private readonly object _lockObject = new();

        /// <summary>
        /// Get count of all AGVs in the registry
        /// </summary>
        public int Count => _agvs.Count;

        /// <summary>
        /// Get count of idle AGVs in the registry
        /// </summary>
        public int CountIdle => _agvs.Count(s => s.Value.State == AgvState.Idle);

        /// <summary>
        /// Manager for assigning available AGVs
        /// </summary>
        public InMemoryAgvRegistry()
        {
            _agvs = new ConcurrentDictionary<Guid, Common.Messages.AgvManagement.Agv>();
        }

        /// <summary>
        /// Adds an AGV to the available AGVs or updates an existing AGV
        /// </summary>
        public void AddOrUpdate(Guid correlationId, Common.Messages.AgvManagement.Agv newAgv)
        {
            _agvs.AddOrUpdate(correlationId, newAgv, (_, _) => newAgv);
        }

        /// <summary>
        /// Removes an AGV with the given correlationId from registry
        /// </summary>
        /// <returns>true when the AGV to be removed was found, otherwise false</returns>
        public bool Remove2(Guid correlationId)
        {
            return _agvs.Remove(correlationId, out var _);
        }

        /// <summary>
        /// Find and reserve an idle AGV for assignment to an order
        /// </summary>
        public Common.Messages.AgvManagement.Agv? ReserveIdleAgv()
        {
            lock (_lockObject)
            {
                if (_agvs.IsEmpty)
                {
                    return null; // No AGVs available
                }

                foreach (KeyValuePair<Guid, Common.Messages.AgvManagement.Agv> item in _agvs)
                {
                    if (item.Value.State == AgvState.Idle)
                    {
                        item.Value.State = AgvState.Reserved;
                        return item.Value;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Get list of idle AGVs
        /// </summary>
        public IEnumerable<Common.Messages.AgvManagement.Agv> GetIdleAgvs()
        {
            return _agvs.Values.Where(s => s.State == AgvState.Idle);
        }
    }
}