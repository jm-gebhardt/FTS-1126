using System.Collections.Concurrent;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc />
public class AgvPositionDictionaryProvider : IConcurrentDictionaryProvider<Guid, AgvPositionStatus>
{
    /// <inheritdoc />
    public ConcurrentDictionary<Guid, AgvPositionStatus> ConcurrentDict { get; } = new();
}