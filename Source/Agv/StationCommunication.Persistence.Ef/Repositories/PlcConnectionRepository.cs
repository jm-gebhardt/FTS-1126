using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using JetBrains.Annotations;

namespace Gebhardt.Acc.StationCommunication.Persistence.Ef.Repositories;

/// In-Memory implementation of <inheritdoc cref="IPlcConnectionRepository"/>
[UsedImplicitly]
public class PlcConnectionRepository(
    IConcurrentDictionaryProvider<string, bool> channelState,
    IConcurrentDictionaryProvider<Guid, bool> stationState) : IPlcConnectionRepository
{
    private readonly TaskCompletionSource _initializationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private HashSet<string> _activeChannelKeys = [];
    private Dictionary<string, List<Guid>> _stationsByChannel = new();
    private Dictionary<Guid, List<string>> _channelsByStation = new();

    /// <inheritdoc />
    public void Initialize(ICollection<Station> stations, IEnumerable<string> activeChannelKeys)
    {
        _activeChannelKeys = [..activeChannelKeys];

        foreach (string channelKey in _activeChannelKeys)
        {
            channelState.ConcurrentDict.TryAdd(channelKey, false);
        }

        var newStationsByChannel = new Dictionary<string, List<Guid>>();
        var newChannelsByStation = new Dictionary<Guid, List<string>>();

        foreach (var station in stations)
        {
            List<string> stationChannelKeys = station.PlcConnections
                .Where(ch => _activeChannelKeys.Contains(ch))
                .ToList();

            if (stationChannelKeys.Count == 0)
            {
                continue;
            }

            var stationId = station.Name.ToGuid();
            newChannelsByStation[stationId] = stationChannelKeys;

            foreach (string channelKey in stationChannelKeys)
            {
                if (!newStationsByChannel.TryGetValue(channelKey, out List<Guid>? stationIds))
                {
                    newStationsByChannel[channelKey] = stationIds = [];
                }

                stationIds.Add(stationId);
            }
        }

        _stationsByChannel = newStationsByChannel;
        _channelsByStation = newChannelsByStation;
        _initializationTcs.TrySetResult();
    }

    /// <inheritdoc />
    public Task WaitForInitializationAsync(CancellationToken cancellationToken) => _initializationTcs.Task.WaitAsync(cancellationToken);

    /// <inheritdoc />
    public IEnumerable<(Guid stationId, bool isOnline)> GetAllStationStates() =>
        stationState.ConcurrentDict.Select(kvp => (kvp.Key, kvp.Value));

    /// <inheritdoc />
    public IEnumerable<(Guid stationId, bool isOnline)> UpdateChannelState(string channelKey, bool isConnected, bool includeUntracked = false)
    {
        if (!_activeChannelKeys.Contains(channelKey))
        {
            yield break;
        }

        channelState.ConcurrentDict[channelKey] = isConnected;

        if (!_stationsByChannel.TryGetValue(channelKey, out List<Guid>? affectedStations))
        {
            yield break;
        }

        foreach (var stationId in affectedStations)
        {
            bool newIsOnline = DeriveStationOnline(stationId);
            bool hasExplicitState = stationState.ConcurrentDict.TryGetValue(stationId, out bool previousIsOnline);
            bool shouldYield = (!hasExplicitState && includeUntracked) || previousIsOnline != newIsOnline;

            stationState.ConcurrentDict[stationId] = newIsOnline;

            if (shouldYield)
            {
                yield return (stationId, newIsOnline);
            }
        }
    }

    private bool DeriveStationOnline(Guid stationId)
    {
        if (!_channelsByStation.TryGetValue(stationId, out List<string>? channels))
        {
            return false;
        }

        return channels.All(ch => channelState.ConcurrentDict.GetValueOrDefault(ch));
    }
}