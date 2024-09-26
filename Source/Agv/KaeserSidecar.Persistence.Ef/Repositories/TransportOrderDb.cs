using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITransportOrderDb"/>>
public class TransportOrderDb: ITransportOrderDb
{
    /// <inheritdoc cref="ITransportOrderDb"/>>
    public TransportOrderDb(ITransportOrderActionGroupRepository actionGroups, ITransportOrderAgvRepository agvs, ITransportOrderStationRepository stations)
    {
        ActionGroups = actionGroups;
        Agvs = agvs;
        Stations = stations;
    }

    /// <inheritdoc cref="ITransportOrderDb"/>>
    public Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="ITransportOrderDb"/>>
    public ITransportOrderActionGroupRepository ActionGroups { get; }
    
    /// <inheritdoc cref="ITransportOrderDb"/>>

    public ITransportOrderAgvRepository Agvs { get; }

    /// <inheritdoc cref="ITransportOrderDb"/>>
    public ITransportOrderStationRepository Stations { get; }
}