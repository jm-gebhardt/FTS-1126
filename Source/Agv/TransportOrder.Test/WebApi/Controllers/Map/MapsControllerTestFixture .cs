using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Map;

public class MapsControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, MapsController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private const string Base64String = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private readonly Mock<IGraphRepository> _agvGraphRepositoryMock = new();

    private readonly IMapRepository _mapRepository;
    private readonly IAgvRepository _agvRepository;

    private readonly string[] _agvSerialNumbers = { "AGV001", "AGV002", "AGV003" };
    private readonly string[] _orders = { "ORDER001", "ORDER002", "" };
    private readonly AgvState[] _states = { AgvState.Idle, AgvState.Busy, AgvState.Discovered };

    private readonly Guid[] _correlationIds =
    {
        new("7FE9372B-4219-4D09-8885-966EE125A350"),
        new("041AD79E-1952-4AB7-BD3E-9EC972E27FBF"),
        new("465A85A6-6A02-4068-A7AA-E3B4B9821019")
    };

    public MapsControllerTestFixture()
    {
        _mapRepository = GetMapRepository().Object;
        _agvRepository = GetAgvRepository().Object;
    }

    protected override MapsController GetController()
    {
        IRequestClient<GetMapList> getMapListRequestClient = GetRequestClient<GetMapList>();

        var controller = new MapsController(getMapListRequestClient);

        return controller;
    }
    
    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new MapProfile(),
        new Gebhardt.Acc.TransportOrder.Messages.Profiles.MapProfile()
    ];

    private Mock<IMapRepository> GetMapRepository()
    {
        var mapRepositoryMock = new Mock<IMapRepository>();

        mapRepositoryMock.Setup(x => x.GetAllMapsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Map.Map>(new List<TransportOrder.Domain.Models.Map.Map>
        {
            MapGenerator.Generate(_correlationIds[0], "Area51_Floor1_Compartment47", Convert.FromBase64String(Base64String), 1, 1, 0.01, 1, 2, DateTime.UtcNow),
            MapGenerator.Generate(_correlationIds[1], "Area51_Floor1_Compartment48", Convert.FromBase64String(Base64String), 1, 1, 0.02, 2, 3, DateTime.UtcNow),  
            MapGenerator.Generate(_correlationIds[2], "Area51_Floor1_Compartment49", Convert.FromBase64String(Base64String), 1, 1, 0.03, 2, 1, DateTime.UtcNow)
        }));
        mapRepositoryMock.Setup(x => x.GetMapImage(It.IsAny<Guid>())).Returns(Convert.FromBase64String(Base64String));

        return mapRepositoryMock;
    }

    private Mock<IAgvRepository> GetAgvRepository()
    {
        var repositoryMock = new Mock<IAgvRepository>();

        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv>();

        for (int i = 0; i < _agvSerialNumbers.Length; i++)
        {
            string agvSerialNumber = _agvSerialNumbers[i];
            string orderId = _orders[i];
            var state = _states[i];
            var correlationId = _correlationIds[i];
            var agvStatus = new AgvStatus(correlationId, orderId, true, true, 10, true, true);

            var agv = AgvGenerator.GenerateRawAgv(correlationId, agvSerialNumber, agvStatus);
            agv = AgvGenerator.SetAgvState(state, agv, false);

            agvs.Add(agv);
        }

        repositoryMock.AddAgvs(agvs.ToArray());
        return repositoryMock;
    }

    /// <summary>
    /// Adds the <see cref="IMapRepository"/> mock object
    /// </summary>
    /// <returns></returns>
    [UsedImplicitly]
    public MapsControllerTestFixture<TRequest, TRequestHandler> AddMapRepositoryMock(Action<Mock<IMapRepository>> mapRepositoryAction)
    {
        var mapRepositoryMock = new Mock<IMapRepository>();
        mapRepositoryAction.Invoke(mapRepositoryMock);

        AddServices(sc => sc.AddTransient(_ => mapRepositoryMock.Object));

        return this;
    }

    public new MapsControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _mapRepository);
            sc.AddTransient(_ => _agvRepository);
            sc.AddTransient(_ => _agvGraphRepositoryMock.Object);
        });

        base.Build();
        return this;
    }
}