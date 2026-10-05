using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Services;

/// <inheritdoc cref="IZoneRefreshStateService"/>>
[UsedImplicitly]
public class ZoneRefreshStateService(ILogger<ZoneRefreshStateService> logger) : IZoneRefreshStateService
{
    private readonly ConcurrentDictionary<Guid, OfflineAgvInZones> _offlineAgvsInZones = new();
    private volatile string[] _availableOnlineAgvs = Array.Empty<string>();
    private int _currentTargetIndex;

    /// <inheritdoc cref="IZoneRefreshStateService"/>>
    public Task AddOfflineAgvInZones(Guid correlationId, string serialNumber, ICollection<string> zones, ICollection<string> currentOnlineAgvs)
    {
        var offlineAgv = new OfflineAgvInZones(correlationId, serialNumber, zones, DateTime.UtcNow);

        _offlineAgvsInZones.AddOrUpdate(correlationId, offlineAgv, (_, _) => offlineAgv); 

        _availableOnlineAgvs = currentOnlineAgvs.ToArray();

        logger.LogInformation(
            "AGV {SerialNumber} ({AgvId}) went offline in zones: {Zones}. Total offline AGVs: {Count}, Online AGVs: {OnlineCount}",
            serialNumber, correlationId, string.Join(", ", zones),
            _offlineAgvsInZones.Count, currentOnlineAgvs.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IZoneRefreshStateService"/>>
    public Task SetAgvBackOnline(Guid correlationId)
    {
        if (!_offlineAgvsInZones.TryGetValue(correlationId, out var offlineAgv))
        {
            return Task.CompletedTask;
        }

        AddToOnlineAgvs(offlineAgv.SerialNumber);

        bool wasRemoved = _offlineAgvsInZones.TryRemove(correlationId, out var removedAgv);
        if (wasRemoved && removedAgv != null)
        {
            logger.LogInformation("AGV ({AgvId}) came back online. Remaining offline AGVs: {Count}", correlationId, _offlineAgvsInZones.Count);
        }
        return Task.CompletedTask;
    }

    private void AddToOnlineAgvs(string agvId)
    {
        while (true)
        {
            string[] currentOnlineAgvs = _availableOnlineAgvs;

            if (currentOnlineAgvs.Contains(agvId))
            {
                return;
            }

            string[] newOnlineAgvs = currentOnlineAgvs.Append(agvId).ToArray();

            if (Interlocked.CompareExchange(ref _availableOnlineAgvs, newOnlineAgvs, currentOnlineAgvs) == currentOnlineAgvs)
            {
                return;
            }
        }
    }

    /// <inheritdoc cref="IZoneRefreshStateService"/>>
    public Task<Dictionary<Guid, ICollection<string>>> UpdateAgvsFromRefreshedZones(ICollection<MqttZone> zones)
    {
        var removedZonesByAgv = new Dictionary<Guid, ICollection<string>>();

        var currentOccupancyByAgvSerialNumber = zones
            .SelectMany(zone => zone.Occupancy.Select(serial => new { SerialNumber = serial, zone.ZoneId }))
            .GroupBy(x => x.SerialNumber)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ZoneId).ToHashSet());

        foreach (var (agvId, offlineAgv) in _offlineAgvsInZones.ToList())
        {
            // Current zones for this agv
            var currentZonesForAgv = currentOccupancyByAgvSerialNumber.GetValueOrDefault(offlineAgv.SerialNumber, new HashSet<string>());

            // Removed zones
            var removedFromZones = offlineAgv.UpdateZones(currentZonesForAgv, out var _);

            if (removedFromZones.Count > 0)
            {
                removedZonesByAgv[offlineAgv.AgvCorrelationId] = removedFromZones;
            }

            if (!offlineAgv.IsInAnyZone)
            {
                _offlineAgvsInZones.TryRemove(agvId, out _);
                logger.LogInformation("Offline AGV {SerialNumber} is no longer in any zones, removing from tracking", offlineAgv.SerialNumber);
                continue;
            }
            logger.LogInformation("Offline AGV {SerialNumber} is still in zones {Zones}", offlineAgv.SerialNumber, string.Join(",", offlineAgv.CurrentZones.ToList()));
        }

        return Task.FromResult(removedZonesByAgv);
    }

    /// <inheritdoc cref="IZoneRefreshStateService"/>>
    public bool ZoneRefreshRequired
    {
        get
        {
            string[] onlineAgvs = _availableOnlineAgvs;
            return _offlineAgvsInZones.Any() && onlineAgvs.Length > 0;
        }
    }

    /// <inheritdoc cref="IZoneRefreshStateService"/>>
    public string? GetNextOnlineAgv()
    {
        string[] onlineAgvs = _availableOnlineAgvs; 
        if (onlineAgvs.Length == 0)
        {
            return null;
        }

        // Thread-safe index handling
        int index = Interlocked.Increment(ref _currentTargetIndex) - 1;
        int targetIndex = index % onlineAgvs.Length;

        return onlineAgvs[targetIndex];
    }

    /// <inheritdoc cref="IZoneRefreshStateService"/>>
    public ICollection<OfflineAgvInZones> OfflineAgvsInZones => _offlineAgvsInZones.Values.ToList();
}