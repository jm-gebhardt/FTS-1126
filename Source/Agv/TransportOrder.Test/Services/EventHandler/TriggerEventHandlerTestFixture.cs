using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Cargo = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Cargo;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

public class TriggerEventHandlerTestFixture : MassTransitConsumerTestFixture<TriggerEventHandler>
{
    private Mock<IAgvDb> _agvDb = null!;
    private Mock<ITriggerRepository> _triggerRepository = null!;
    private Mock<IGraphRepository> _graphRepository = null!;

    protected override IEnumerable<Profile> GetProfiles() => [new AgvProfile(), new CargoProfile(), new AgvStatusProfile()];

    public TriggerEventHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<TriggerEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);

        _agvDb = new Mock<IAgvDb>();
        _triggerRepository = new Mock<ITriggerRepository>();
        _graphRepository = new Mock<IGraphRepository>();

        collection.AddSingleton(_agvDb.Object);
        collection.AddSingleton(_triggerRepository.Object);
        collection.AddSingleton(_graphRepository.Object);

        return this;
    }

    public TriggerEventHandlerTestFixture WithAgv(Agv agv)
    {
        _agvDb.AddAgvs(agv);
        return this;
    }

    public TriggerEventHandlerTestFixture WithCargo(Guid agvId, Cargo cargo)
    {
        _agvDb.Setup(a => a.Cargos.GetCargosAsync(agvId)).ReturnsAsync([cargo]);
        return this;
    }

    public TriggerEventHandlerTestFixture WithPosition(Guid agvId, AgvPositionStatus? position)
    {
        _agvDb.Setup(a => a.Positions.GetPosition(agvId)).Returns(position);
        return this;
    }

    public TriggerEventHandlerTestFixture WithAgvData(Guid agvId, AgvData? data)
    {
        _agvDb.Setup(a => a.Data.GetAgvDataAsync(agvId)).ReturnsAsync(data);
        return this;
    }

    public TriggerEventHandlerTestFixture WithAgvErrors(Guid agvId, IList<AgvError> errors)
    {
        _agvDb.Setup(a => a.Errors.GetActiveErrorsAsync(agvId)).ReturnsAsync(errors);
        return this;
    }

    public TriggerEventHandlerTestFixture WithPolygonTriggerLocations(IList<Location> locations)
    {
        _triggerRepository.Setup(t => t.GetPolygonTriggerLocations(It.IsAny<AgvPosition>())).Returns(locations);
        return this;
    }

    public TriggerEventHandlerTestFixture WithNodeTriggerLocations(IList<Location> locations, string nodeId)
    {
        _triggerRepository.Setup(t => t.GetNodeTriggerLocations(nodeId)).Returns(locations);
        return this;
    }

    public TriggerEventHandlerTestFixture WithNode(GraphNode node)
    {
        _graphRepository.Setup(g => g.TryGetNodeAsync(node.Id)).ReturnsAsync(node);
        return this;
    }
}