using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for hasAgvCargo and GetCargo requests
/// </summary>
[UsedImplicitly]
public class CargoRequestHandler : IConsumer<HasAgvCargoRequest>, IConsumer<GetCargosOfAgv>, IConsumer<GetCargosOfAgvs>
{
    private readonly IAgvDb _agvDb;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="CargoRequestHandler"/>
    public CargoRequestHandler(IAgvDb agvDb, IMapper mapper)
    {
        _agvDb = agvDb;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<HasAgvCargoRequest> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        bool isAgvLoaded = await _agvDb.Cargos.IsAgvLoadedAsync(agvCorrelationId);
        if (!isAgvLoaded)
        {
            await _agvDb.Agvs.ThrowIfAgvNotExistsAsync(agvCorrelationId);
        }
        await context.RespondAsync(new HasAgvCargoResult(isAgvLoaded));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetCargosOfAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        IReadOnlyCollection<Cargo> cargos = await _agvDb.Cargos.GetCargosAsync(agvCorrelationId);
        if (!cargos.Any())
        {
            await _agvDb.Agvs.ThrowIfAgvNotExistsAsync(agvCorrelationId);
        }
        var message = _mapper.Map<ICollection<Common.Messages.AgvManagement.Cargo>>(cargos);
        await context.RespondAsync(new GotCargosOfAgv(message));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetCargosOfAgvs> context)
    {
        ICollection<Guid> agvCorrelationIds = context.Message.CorrelationIds;
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Cargo>> cargosByAgvCorrelationId = await _agvDb.Cargos.GetCargosAsync(agvCorrelationIds);
        IEnumerable<Guid> empty = cargosByAgvCorrelationId.Where(kvp => kvp.Value.Any()).Select(kvp => kvp.Key);
        List<Guid> agvsWithoutCargo = agvCorrelationIds.Except(empty).ToList();
        if (agvsWithoutCargo.Any())
        {
            await _agvDb.Agvs.ThrowIfAgvsNotExistAsync(agvsWithoutCargo);
        }
        var message = _mapper.Map<IReadOnlyDictionary<Guid, ICollection<Common.Messages.AgvManagement.Cargo>>>(cargosByAgvCorrelationId);
        await context.RespondAsync(new GotCargosOfAgvs(message));
    }
}