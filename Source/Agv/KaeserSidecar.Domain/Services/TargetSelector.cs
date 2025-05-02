using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <inheritdoc cref="TargetSelector"/>>
[UsedImplicitly]
public class TargetSelector(
    ILogger<TargetSelector> logger,
    IStationRepository stationRepository,
    KaeserGraphConfiguration configuration,
    ITransportOrderDb transportOrderDb,
    ISemaphoreProvider<ITargetSelectionSemaphore> semaphoreProvider) : ITargetSelector
{
    private string _lastTargetStation = string.Empty;

    /// <inheritdoc cref="TargetSelector"/>>
    public async Task<string?> Select(int consideredStations)
    {
        return await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            string? selectedTarget = null;
            while (selectedTarget == null)
            {
                ICollection<StationOccupancy> occupancies = await stationRepository.GetStationOccupancy();
                ICollection<TransportOrder.Domain.Models.Station.Station> stations = await transportOrderDb.Stations.GetStationsByNames(configuration.ConveyorStationMapping.Keys.ToArray());

                //Try to take the station that is not occupied, is not locked,  and was not chosen last (to compensate for the delay between setting target and starting action)
                ICollection<StationOccupancy> availableStations = GetAvailableStations(consideredStations, occupancies, stations);
                foreach (var occupancy in availableStations)
                {
                    if (occupancy.Count != 0 || (_lastTargetStation == occupancy.Station && availableStations.Count > 1))
                    {
                        logger.LogInformation("No free station found: {Station} is occupied by {occupancy.Count}, last chosen station {LastTarget}", occupancy.Station, occupancy.Count, _lastTargetStation);
                        continue;
                    }
                    selectedTarget = occupancy.Station;
                    break;
                }

                if (selectedTarget == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
            _lastTargetStation = selectedTarget;
            return selectedTarget;
        });
    }

    private ICollection<StationOccupancy> GetAvailableStations(int consideredStations, ICollection<StationOccupancy> unorderedOccupancies, ICollection<TransportOrder.Domain.Models.Station.Station> stations)
    {
        // By agreement conveyor stations are named "PS01", "PS02", "PS03", thus ordering by conveyor station
        // name ensures priority of PS01 over PS02 and PS03
        IOrderedEnumerable<KeyValuePair<string, string>> orderedConveyorStations = configuration.ConveyorStationMapping.OrderBy(m => m.Value);
        ICollection<StationOccupancy> orderedOccupancies = [];
        foreach (KeyValuePair<string, string> conveyorStationMapping in orderedConveyorStations.Take(consideredStations))
        {
            var occupancy = unorderedOccupancies.SingleOrDefault(o => o.Station == conveyorStationMapping.Key) ??
                            new StationOccupancy(conveyorStationMapping.Key, 0, null);
            if (!stations.First(s => s.Name == occupancy.Station).IsLocked)
            {
                orderedOccupancies.Add(occupancy);
            }
        }
        return orderedOccupancies;
    }
}