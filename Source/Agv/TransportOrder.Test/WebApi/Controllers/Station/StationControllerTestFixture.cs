using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Station;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Station;

public class StationControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, StationController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    [UsedImplicitly]
    private readonly Mock<IAgvDb> _agvDb = new();

    [UsedImplicitly]
    private readonly Mock<IGraphRepository> _agvGraphRepository = new();

    [UsedImplicitly]
    private readonly Mock<IMapRepository> _mapRepository = new();

    protected override StationController GetController()
    {
        IRequestClient<GetStation> getStationRequestClient = GetRequestClient<GetStation>();
        var controller = new StationController(Mapper, getStationRequestClient);
        return controller;
    }

    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new StationProfile()
    ];

    public new StationControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvDb.Object);
            sc.AddTransient(_ => _mapRepository.Object);
            sc.AddTransient(_ => _agvGraphRepository.Object);
        });

        base.Build();
        return this;
    }

    public StationControllerTestFixture<TRequest, TRequestHandler> WithGraphNode(GraphNode graphNode)
    {
        _agvDb.Setup(x => x.Graphs.GetNodeAsync(It.IsAny<string>())).ReturnsAsync(graphNode);
        _agvDb.Setup(x => x.Graphs.ExistsAsync()).ReturnsAsync(true);
        _agvDb.Setup(x => x.Data.GetLargestAgvDimensionAsync()).ReturnsAsync((0, 0));
        _agvGraphRepository.Setup(x => x.GetNodeAsync(It.IsAny<string>())).ReturnsAsync(graphNode);
        return this;
    }
}