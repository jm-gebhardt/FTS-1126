using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class CargoRequestHandlerTestFixture<TRequest, TConsumer> : MediatorTestFixture<TRequest, TConsumer>
    where TRequest : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<ICargoRepository> _cargoRepositoryMock = new();
    private readonly Mock<IAgvRepository> _agvRepositoryMock = new();
    private readonly IMapper _mapper = MappingHelper.GetMapper(new CargoProfile());
    private readonly List<Guid> _existingAgvs = new();
    private readonly Dictionary<Guid, IReadOnlyCollection<TransportOrder.Domain.Models.Agv.Cargo>> _cargosByAgv = new();

    public CargoRequestHandlerTestFixture()
    {
        SetupAgvRepositoryMock();

        SetupCargoRepositoryMock();

        AddServices(s =>
        {
            s.AddTransient<ICargoRepository>(_ => _cargoRepositoryMock.Object);
            s.AddTransient<IAgvRepository>(_ => _agvRepositoryMock.Object);
            s.AddTransient(_ => _mapper);
        });
    }

    private void SetupCargoRepositoryMock()
    {
        _cargoRepositoryMock.Setup(x => x.GetCargosAsync(It.IsAny<Guid>())).ReturnsAsync((Guid correlationId) => _cargosByAgv.TryGetValue(correlationId, out IReadOnlyCollection<TransportOrder.Domain.Models.Agv.Cargo>? cargos) ? cargos : new List<TransportOrder.Domain.Models.Agv.Cargo>());
        _cargoRepositoryMock.Setup(x => x.GetCargosAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync((IEnumerable<Guid> correlationIds) => _cargosByAgv.Where(kvp => correlationIds.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    private void SetupAgvRepositoryMock()
    {
        _agvRepositoryMock.Setup(a => a.ThrowIfAgvNotExistsAsync(It.IsAny<Guid>())).Callback<Guid>(correlationId =>
        {
            if (!_existingAgvs.Contains(correlationId))
            {
                throw new AgvNotFoundException(correlationId);
            }
        });

        _agvRepositoryMock.Setup(a => a.ThrowIfAgvsNotExistAsync(It.IsAny<ICollection<Guid>>())).Callback<ICollection<Guid>>(requestedIds =>
        {
            List<Guid> notExistingAgvs = requestedIds.Except(_existingAgvs).ToList();
            if (notExistingAgvs.Any())
            {
                throw new AgvsNotFoundException(notExistingAgvs);
            }
        });
    }

    private void WithAgvExisting(Guid correlationId)
    {
        _existingAgvs.Add(correlationId);
    }

    public CargoRequestHandlerTestFixture<TRequest, TConsumer> WithCargo(Guid agvCorrelationId, ICollection<Cargo> cargos)
    {
        var domainCargo = _mapper.Map<IReadOnlyCollection<TransportOrder.Domain.Models.Agv.Cargo>>(cargos);
        _cargosByAgv[agvCorrelationId] = domainCargo;
        SetupIsAgvLoaded(agvCorrelationId, domainCargo.ToList());
        WithAgvExisting(agvCorrelationId);
        return this;
    }

    public CargoRequestHandlerTestFixture<TRequest, TConsumer> WithCargos(Dictionary<Guid, ICollection<Cargo>> cargosByAgv)
    {
        foreach (KeyValuePair<Guid, ICollection<Cargo>> kv in cargosByAgv)
        {
            WithCargo(kv.Key, kv.Value.ToList());
        }

        return this;
    }

    private void SetupIsAgvLoaded(Guid agvCorrelationId, ICollection<TransportOrder.Domain.Models.Agv.Cargo> cargos)
    {
        _cargoRepositoryMock.Setup(x => x.IsAgvLoadedAsync(agvCorrelationId)).ReturnsAsync(cargos.Any());
    }

    public override CargoRequestHandlerTestFixture<TRequest, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}