using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;

namespace Gebhardt.Acc.StationCommunication.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfStationRepository(StationConfiguration stationConfiguration): IStationRepository
{
    /// <inheritdoc/>
    public Task<Station?> GetStationByName(string name)
    {
        var station = stationConfiguration.Stations.SingleOrDefault(s => s.Name == name);
        return Task.FromResult(station);
    }
}