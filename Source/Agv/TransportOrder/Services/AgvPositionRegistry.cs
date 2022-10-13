using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using System.Collections.Concurrent;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Registry of AGV positions (in memory)
    /// </summary>
    public class AgvPositionRegistry : BaseAgvPositionRegistry
    {
        private readonly ConcurrentDictionary<Guid, AgvPositionReceived> _agvPositionRegistry = new();

        /// <inheritdoc/>
        public AgvPositionRegistry(IBus bus, IChangeDetector<AgvPositionReceived> agvPositionReceivedChangeDetector) : base(bus, agvPositionReceivedChangeDetector)
        {

        }

        /// <inheritdoc />
        protected override void UpdateInternal(AgvPositionReceived agvPositionReceived)
        {
            var correlationId = agvPositionReceived.CorrelationId;
            _agvPositionRegistry.AddOrUpdate(correlationId, agvPositionReceived, (_, _) => agvPositionReceived);
        }

        /// <inheritdoc />
        public override AgvPositionReceived? GetPosition(Guid correlationId)
        {
            _agvPositionRegistry.TryGetValue(correlationId, out var agvPositionReceived);
            return agvPositionReceived;
        }

        /// <inheritdoc />
        public override List<AgvPositionReceived> GetAllPositions()
        {
            return _agvPositionRegistry.Values.ToList();
        }
    }
}
