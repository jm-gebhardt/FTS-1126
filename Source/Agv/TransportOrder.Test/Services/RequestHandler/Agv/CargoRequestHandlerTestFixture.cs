using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

public class CargoRequestHandlerTestFixture<TRequest, TConsumer> : MediatorTestFixture<TRequest, TConsumer, MockConsumer>
    where TRequest : class
    where TConsumer : class, IConsumer
{
    public readonly Mock<IAgvDb> AgvDb = new();
    private readonly List<TransportOrder.Domain.Models.Agv.Agv> _agvs = new();
    private readonly Dictionary<Guid, IReadOnlyCollection<Cargo>> _cargosByAgv = new();

    private static IEnumerable<Profile> Profiles => [new CargoProfile()];

    protected override IEnumerable<Profile> GetProfiles() => Profiles;

    public CargoRequestHandlerTestFixture()
    {
        SetupCargoRepositoryMock();

        AddServices(s => { s.AddTransient<IAgvDb>(_ => AgvDb.Object); });
    }

    private void SetupCargoRepositoryMock()
    {
        AgvDb.Setup(x => x.Cargos.GetCargosAsync(It.IsAny<Guid>())).ReturnsAsync((Guid correlationId) => _cargosByAgv.TryGetValue(correlationId, out var cargos) ? cargos : new List<Cargo>());
        AgvDb.Setup(x => x.Cargos.GetCargosAsync((ICollection<Guid>)It.IsAny<IEnumerable<Guid>>())).ReturnsAsync((IEnumerable<Guid> correlationIds) => _cargosByAgv.Where(kvp => correlationIds.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        AgvDb.Setup(x => x.Cargos.HaveAgvsCargoAsync(It.IsAny<ICollection<Guid>>()))
            .ReturnsAsync((ICollection<Guid> correlationIds) =>
                _cargosByAgv.Where(kvp => correlationIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count != 0));
    }

    private void SetupAgvRepositoryMock()
    {
        AgvDb.AddAgvs(_agvs);
    }
    
    private void WithAgvExisting(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvs.Add(agv);
    }

    public CargoRequestHandlerTestFixture<TRequest, TConsumer> WithCargo(Guid agvCorrelationId, ICollection<Common.Domain.Models.Cargo> cargos)
    {
        var domainCargo = Mapper.Map<IReadOnlyCollection<Cargo>>(cargos);
        _cargosByAgv[agvCorrelationId] = domainCargo;
        SetupIsAgvLoaded(agvCorrelationId, domainCargo.ToList());
        
        var agv = AgvGenerator.GenerateRawAgv(agvCorrelationId.ToString());
        WithAgvExisting(agv);
        
        return this;
    }

    public CargoRequestHandlerTestFixture<TRequest, TConsumer> WithCargos(Dictionary<Guid, ICollection<Common.Domain.Models.Cargo>> cargosByAgv)
    {
        foreach (var kv in cargosByAgv)
        {
            WithCargo(kv.Key, kv.Value.ToList());
        }

        return this;
    }

    private void SetupIsAgvLoaded(Guid agvCorrelationId, ICollection<Cargo> cargos)
    {
        AgvDb.Setup(x => x.Cargos.HasAgvCargoAsync(agvCorrelationId)).ReturnsAsync(cargos.Count != 0);
    }

    public override CargoRequestHandlerTestFixture<TRequest, TConsumer> Build()
    {
        SetupAgvRepositoryMock();
        base.Build();
        return this;
    }
}