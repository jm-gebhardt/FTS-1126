using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef;

/// <inheritdoc cref="IAgvDb"/>>
public class AgvDb : IAgvDb
{
    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvDataRepository Data { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvTypeRepository AgvTypes { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvErrorRepository Errors { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public ICargoRepository Cargos { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IGraphRepository Graphs { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public INewGraphRepository NewGraphs { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvRepository Agvs { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvSettingsProvider AgvSettings { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IZoneRepository Zones { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvPositionRepository Positions { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IMaintenanceLogRepository MaintenanceLogs { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public AgvDb(IAgvDataRepository data,
        IAgvTypeRepository agvTypes,
        IAgvErrorRepository errors,
        ICargoRepository cargos,
        IGraphRepository graphs,
        INewGraphRepository newGraphs,
        IAgvRepository agvs,
        IAgvSettingsProvider settings,
        IZoneRepository zones,
        IAgvPositionRepository positions,
        IMaintenanceLogRepository maintenanceLogs)
    {
        Data = data;
        AgvTypes = agvTypes;
        Errors = errors;
        Cargos = cargos;
        Graphs = graphs;
        NewGraphs = newGraphs;
        Agvs = agvs;
        AgvSettings = settings;
        Zones = zones;
        Positions = positions;
        MaintenanceLogs = maintenanceLogs;
    }

    /// <inheritdoc cref="IAgvDb"/>>
    public Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}