using System.Collections.Concurrent;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <inheritdoc />
public class AgvPositionDictionaryProvider : IConcurrentDictionaryProvider<Guid, AgvPositionStatus>
{
   /// <inheritdoc />
    public ConcurrentDictionary<Guid, AgvPositionStatus> ConcurrentDict { get; } = new();
}