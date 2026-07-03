using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;

namespace Gebhardt.Acc.TransportOrder.Domain;

/// <summary>
/// Provides a unit-of-work for agv access.
/// </summary>
public interface IAgvDb : IUnitOfWork

{
    /// <summary>
    /// Gets the <see cref="IAgvDataRepository"/>.
    /// </summary>
    IAgvDataRepository Data { get; }

    /// <summary>
    /// Gets the <see cref="IAgvTypeRepository"/>.
    /// </summary>
    IAgvTypeRepository AgvTypes { get; }

    /// <summary>
    /// Gets the <see cref="IAgvErrorRepository"/>.
    /// </summary>
    IAgvErrorRepository Errors { get; }

    /// <summary>
    /// Gets the <see cref="ICargoRepository"/>.
    /// </summary>
    ICargoRepository Cargos { get; }

    /// <summary>
    /// Gets the <see cref="IGraphRepository"/>.
    /// </summary>
    IGraphRepository Graphs { get; }

    /// <summary>
    /// Gets the <see cref="IAgvRepository"/>.
    /// </summary>
    IAgvRepository Agvs { get; }

    /// <summary>
    /// Gets the <see cref="IAgvSettingsProvider"/>.
    /// </summary>
    IAgvSettingsProvider AgvSettings { get; }

    /// <summary>
    /// Gets the <see cref="IZoneRepository"/>.
    /// </summary>
    IZoneRepository Zones { get; }

    /// <summary>
    /// Gets the <see cref="IAgvPositionRepository"/>.
    /// </summary>
    public IAgvPositionRepository Positions { get; }

    /// <summary>
    /// Gets the <see cref="IMaintenanceLogRepository"/>.
    /// </summary>
    public IMaintenanceLogRepository MaintenanceLogs { get; }
}