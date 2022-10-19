using System.Collections.Concurrent;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Registry of AGV positions (in memory)
    /// </summary>
    public class AgvPositionRegistry : BaseAgvPositionRegistry
    {
        private readonly ConcurrentDictionary<Guid, AgvPositionStatus> _agvPositionRegistry = new();

        /// <inheritdoc/>
        public AgvPositionRegistry(IBus bus, IChangeDetector<AgvPositionStatus> agvPositionChangeDetector) : base(bus, agvPositionChangeDetector)
        {

        }

        /// <inheritdoc />
        protected override void UpdateInternal(AgvPositionStatus agvPositionStatus)
        {
            var correlationId = agvPositionStatus.CorrelationId;
            _agvPositionRegistry.AddOrUpdate(correlationId, agvPositionStatus, (_, _) => agvPositionStatus);
        }

        /// <inheritdoc />
        public override AgvPositionStatus? GetPosition(Guid correlationId)
        {
            _agvPositionRegistry.TryGetValue(correlationId, out var agvPositionReceived);
            return agvPositionReceived;
        }

        /// <inheritdoc />
        public override List<AgvPositionStatus> GetAllPositions()
        {
            return _agvPositionRegistry.Values.ToList();
        }
    }
}
