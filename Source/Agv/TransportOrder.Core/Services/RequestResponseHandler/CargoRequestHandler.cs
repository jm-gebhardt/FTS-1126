using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for hasAgvCargo and GetCargo requests
/// </summary>
[UsedImplicitly]
public class CargoRequestHandler :
    IConsumer<GetAgvHasCargo>,
    IConsumer<GetAgvsHasCargo>,
    IConsumer<GetCargosOfAgv>,
    IConsumer<GetCargosOfAgvs>
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
    public async Task Consume(ConsumeContext<GetAgvHasCargo> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        bool isAgvLoaded = await _agvDb.Cargos.HasAgvCargoAsync(agvCorrelationId);
        if (!isAgvLoaded)
        {
            await _agvDb.Agvs.ThrowIfAgvNotExistsAsync(agvCorrelationId);
        }
        await context.RespondAsync(new GotAgvHasCargo(new AgvHasCargo(agvCorrelationId, isAgvLoaded)));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetAgvsHasCargo> context)
    {
        ICollection<Guid> agvCorrelationIds = context.Message.CorrelationIds;
        IReadOnlyDictionary<Guid, bool> hasCargoByAgv = await _agvDb.Cargos.HaveAgvsCargoAsync(agvCorrelationIds);
        IEnumerable<Guid> empty = hasCargoByAgv.Where(kvp => !kvp.Value).Select(kvp => kvp.Key);
        List<Guid> agvsWithoutCargo = agvCorrelationIds.Except(empty).ToList();
        if (agvsWithoutCargo.Count != 0)
        {
            await _agvDb.Agvs.ThrowIfAgvsNotExistAsync(agvsWithoutCargo);
        }
        await context.RespondAsync(new GotAgvsHasCargo(hasCargoByAgv.Select(h => new AgvHasCargo(h.Key, h.Value)).ToList()));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetCargosOfAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        IReadOnlyCollection<Cargo> cargos = await _agvDb.Cargos.GetCargosAsync(agvCorrelationId);
        if (cargos.Count == 0)
        {
            await _agvDb.Agvs.ThrowIfAgvNotExistsAsync(agvCorrelationId);
        }
        var message = _mapper.Map<ICollection<Common.Domain.Models.Cargo>>(cargos);
        await context.RespondAsync(new GotCargosOfAgv(message));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetCargosOfAgvs> context)
    {
        ICollection<Guid> agvCorrelationIds = context.Message.CorrelationIds;
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Cargo>> cargosByAgvCorrelationId = await _agvDb.Cargos.GetCargosAsync(agvCorrelationIds);
        IEnumerable<Guid> empty = cargosByAgvCorrelationId.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key);
        List<Guid> agvsWithoutCargo = agvCorrelationIds.Except(empty).ToList();
        if (agvsWithoutCargo.Count != 0)
        {
            await _agvDb.Agvs.ThrowIfAgvsNotExistAsync(agvsWithoutCargo);
        }
        var messageDict = _mapper.Map<IReadOnlyDictionary<Guid, ICollection<Common.Domain.Models.Cargo>>>(cargosByAgvCorrelationId);
        IEnumerable<AgvCargos> message = messageDict.Select(kv=> new AgvCargos(kv.Key, kv.Value));

        await context.RespondAsync(new GotCargosOfAgvs(message));
    }
}