using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain;

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
}