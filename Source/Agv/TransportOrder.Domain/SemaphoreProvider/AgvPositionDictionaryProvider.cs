using System.Collections.Concurrent;
using TransportOrder.Domain.Models.Agv;

namespace TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc />
public class AgvPositionDictionaryProvider : IConcurrentDictionaryProvider<Guid, AgvPositionStatus>
{
   /// <inheritdoc />
    public ConcurrentDictionary<Guid, AgvPositionStatus> ConcurrentDict { get; } = new();
}