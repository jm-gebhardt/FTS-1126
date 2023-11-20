using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for IsAgvLoaded and GetCargo requests
/// </summary>
[UsedImplicitly]
public class CargoRequestHandler : IConsumer<IsAgvLoadedRequest>, IConsumer<GetCargosOfAgv>, IConsumer<GetCargosOfAgvs>
{
    private readonly ICargoRepository _cargoRepository;
    private readonly IAgvRepository _agvRepository;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="CargoRequestHandler"/>
    public CargoRequestHandler(ICargoRepository cargoRepository, IAgvRepository agvRepository, IMapper mapper)
    {
        _cargoRepository = cargoRepository;
        _agvRepository = agvRepository;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<IsAgvLoadedRequest> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        bool isAgvLoaded = await _cargoRepository.IsAgvLoadedAsync(agvCorrelationId);
        if (!isAgvLoaded)
        {
            await _agvRepository.ThrowIfAgvNotExistsAsync(agvCorrelationId);
        }
        await context.RespondAsync(new IsAgvLoadedResult(isAgvLoaded));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetCargosOfAgv> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        IReadOnlyCollection<Domain.Models.Agv.Cargo> cargos = await _cargoRepository.GetCargosAsync(agvCorrelationId);
        if (!cargos.Any())
        {
            await _agvRepository.ThrowIfAgvNotExistsAsync(agvCorrelationId);
        }
        var message = _mapper.Map<ICollection<Cargo>>(cargos);
        await context.RespondAsync(new GotCargosOfAgv(message));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetCargosOfAgvs> context)
    {
        ICollection<Guid> agvCorrelationIds = context.Message.CorrelationIds;
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Domain.Models.Agv.Cargo>> cargosByAgvCorrelationId = await _cargoRepository.GetCargosAsync(agvCorrelationIds);
        IEnumerable<Guid> empty = cargosByAgvCorrelationId.Where(kvp => kvp.Value.Any()).Select(kvp => kvp.Key);
        List<Guid> agvsWithoutCargo = agvCorrelationIds.Except(empty).ToList();
        if (agvsWithoutCargo.Any())
        {
            await _agvRepository.ThrowIfAgvsNotExistAsync(agvsWithoutCargo);
        }
        var message = _mapper.Map<IReadOnlyDictionary<Guid, ICollection<Cargo>>>(cargosByAgvCorrelationId);
        await context.RespondAsync(new GotCargosOfAgvs(message));
    }
}