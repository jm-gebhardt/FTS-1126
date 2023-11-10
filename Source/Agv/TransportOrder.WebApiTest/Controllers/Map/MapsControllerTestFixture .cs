using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AgvStatus = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Map;

public class MapsControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, MapsController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private const string Base64String = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private readonly Mock<IAgvGraphRepository> _agvGraphRepository = new();

    private readonly string[] _agvIds = { "AGV001", "AGV002", "AGV003" };
    private readonly string[] _orders = { "ORDER001", "ORDER002", "" };
    private readonly AgvState[] _states = { AgvState.Idle, AgvState.Busy, AgvState.Discovered };

    private readonly Guid[] _correlationIds =
    {
        new("7FE9372B-4219-4D09-8885-966EE125A350"),
        new("041AD79E-1952-4AB7-BD3E-9EC972E27FBF"),
        new("465A85A6-6A02-4068-A7AA-E3B4B9821019")
    };

    protected override MapsController GetController()
    {
        IRequestClient<GetMapList> getMapListRequestClient = GetRequestClient<GetMapList>();

        var controller = new MapsController(getMapListRequestClient);

        return controller;
    }

    private Mock<IMapRepository> GetMapRepository()
    {
        var mapRepositoryMock = new Mock<IMapRepository>();

        mapRepositoryMock.Setup(x => x.GetAllMaps()).Returns(new EnumerableQuery<Domain.Models.Map.Map>(new List<Domain.Models.Map.Map>
        {
            new(_correlationIds[0], "Area51_Floor1_Compartment47", Convert.FromBase64String(Base64String), 1, 1, 0.01, 1, 2, DateTime.UtcNow),
            new(_correlationIds[1], "Area51_Floor1_Compartment48", Convert.FromBase64String(Base64String), 1, 1, 0.02, 2, 3, DateTime.UtcNow),
            new(_correlationIds[2], "Area51_Floor1_Compartment49", Convert.FromBase64String(Base64String), 1, 1, 0.03, 2, 1, DateTime.UtcNow)
        }));
        mapRepositoryMock.Setup(x => x.GetMapImage(It.IsAny<Guid>())).Returns(Convert.FromBase64String(Base64String));

        return mapRepositoryMock;
    }

    private Mock<IAgvRepository> GetAgvRepository()
    {
        var repositoryMock = new Mock<IAgvRepository>();

        var agvs = new List<Domain.Models.Agv.Agv>();

        for (int i = 0; i < _agvIds.Length; i++)
        {
            string agvId = _agvIds[i];
            string orderId = _orders[i];
            var state = _states[i];
            var correlationId = _correlationIds[i];
            var agvStatus = new AgvStatus(correlationId, orderId, true, true, 10, true, true);

            var agv = new Domain.Models.Agv.Agv(correlationId, agvId, agvStatus);
            agv = AgvGenerator.SetAgvState(state, false, agv);

            agvs.Add(agv);
        }

        repositoryMock.SetupGet(x => x.Count).Returns(agvs.Count);
        repositoryMock.Setup(x => x.GetIdleAgvsAsync()).ReturnsAsync(new List<Domain.Models.Agv.Agv>(agvs.Where(x => x.CurrentAgvState == AgvState.Idle)));
        repositoryMock.Setup(x => x.GetAllAgvsAsync()).ReturnsAsync(new List<Domain.Models.Agv.Agv>(agvs));
        repositoryMock.Setup(x => x.GetAgvAsync(It.IsAny<Guid>())).Returns<Guid>(correlationId => Task.FromResult(agvs.Single(a => a.CorrelationId == correlationId)));

        return repositoryMock;
    }

    /// <summary>
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="serviceAction">The action of the service to add</param>
    /// <returns></returns>
    [UsedImplicitly]
    public MapsControllerTestFixture<TRequest, TRequestHandler> AddService(Action<IServiceCollection> serviceAction)
    {
        AddServices(serviceAction);
        return this;
    }

    /// <summary>
    /// Uses the default services
    /// </summary>
    /// <returns></returns>
    public MapsControllerTestFixture<TRequest, TRequestHandler> UseDefaultServices()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => GetMapRepository().Object);
            sc.AddTransient(_ => GetAgvRepository().Object);
            sc.AddTransient(_ => GetMapper());
            sc.AddTransient(_ => _agvGraphRepository.Object);
        });
        return this;
    }

    public MapsControllerTestFixture<TRequest, TRequestHandler> WithAgvGraphs(List<AgvGraph> agvGraphs)
    {
        _agvGraphRepository.Setup(x => x.GetAllAgvGraphsAsync()).ReturnsAsync(agvGraphs);
        return this;
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
        base.Build();
        return this;
    }
}