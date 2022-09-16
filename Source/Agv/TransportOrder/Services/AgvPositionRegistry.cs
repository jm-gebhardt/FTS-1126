using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using System.Collections.Concurrent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Registry of AGV positions (in memory)
    /// </summary>
    public class AgvPositionRegistry : IAgvPositionRegistry
    {
        private readonly ConcurrentDictionary<Guid, AgvPositionReceived> _agvPositionRegistry = new();

        /// <inheritdoc />
        public void UpdatePosition(AgvPositionReceived agvPositionReceived)
        {
            _agvPositionRegistry.AddOrUpdate(agvPositionReceived.CorrelationId, agvPositionReceived, (_, _) => agvPositionReceived);
        }

        /// <inheritdoc />
        public AgvPositionReceived? GetPosition(Guid correlationId)
        {
            _agvPositionRegistry.TryGetValue(correlationId, out var agvPositionReceived);
            return agvPositionReceived;
        }

        /// <inheritdoc />
        public List<AgvPositionReceived> GetAllPositions()
        {
            return _agvPositionRegistry.Values.ToList();
        }
    }
}
