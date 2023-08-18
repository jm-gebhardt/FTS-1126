using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using MassTransit;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Core.Models.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs
{
    /// <summary>
    /// Registry of AGV positions (in memory)
    /// </summary>
    public class AgvPositionRegistry : BaseAgvPositionRegistry
    {
        private readonly IConcurrentDictionaryProvider<Guid, AgvPositionStatus> _positionDictionaryProvider;

        /// <inheritdoc/>
        public AgvPositionRegistry(IBus bus, IChangeDetector<AgvPositionStatus> agvPositionChangeDetector, IConcurrentDictionaryProvider<Guid, AgvPositionStatus> dictionaryProvider) : base(bus, agvPositionChangeDetector)
        {
            _positionDictionaryProvider = dictionaryProvider;
        }

        /// <inheritdoc />
        protected override void UpdateInternal(AgvPositionStatus agvPositionStatus)
        {
            var correlationId = agvPositionStatus.CorrelationId;
            _positionDictionaryProvider.ConcurrentDict.AddOrUpdate(correlationId, agvPositionStatus, (_, _) => agvPositionStatus);
        }

        /// <inheritdoc />
        public override AgvPositionStatus? GetPosition(Guid correlationId)
        {
            _positionDictionaryProvider.ConcurrentDict.TryGetValue(correlationId, out var agvPositionReceived);
            return agvPositionReceived;
        }

        /// <inheritdoc />
        public override List<AgvPositionStatus> GetAllPositions()
        {
            return _positionDictionaryProvider.ConcurrentDict.Values.ToList();
        }
    }
}
