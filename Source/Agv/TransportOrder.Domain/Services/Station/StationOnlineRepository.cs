using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Station;

/// <inheritdoc cref="IStationOnlineRepository"/>
[UsedImplicitly]
public class StationOnlineRepository(StationOnlineDictionaryProvider provider) : IStationOnlineRepository
{
    /// <inheritdoc />
    public bool? Get(Guid stationId) => provider.ConcurrentDict.GetValueOrDefault(stationId);

    /// <inheritdoc />
    public void Update(Guid stationId, bool isOnline)
    {
        provider.ConcurrentDict[stationId] = isOnline;
    }
}