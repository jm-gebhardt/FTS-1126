using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef;

/// <inheritdoc cref="IAgvDb"/>>
public class AgvDb: IAgvDb
{
    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvDataRepository Data { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvErrorRepository Errors { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public ICargoRepository Cargos { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IGraphRepository Graphs { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public IAgvRepository Agvs { get; }

    /// <inheritdoc cref="IAgvDb"/>>
    public AgvDb(IAgvDataRepository data, IAgvErrorRepository errors, ICargoRepository cargos, IGraphRepository graphs, IAgvRepository agvs)
    {
        Data = data;
        Errors = errors;
        Cargos = cargos;
        Graphs = graphs;
        Agvs = agvs;
    }

    /// <inheritdoc cref="IAgvDb"/>>
    public Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}