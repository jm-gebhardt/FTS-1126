using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Map;

public class MapControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, MapController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    public const string Base64String = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [UsedImplicitly]
    private readonly Mock<IAgvGraphRepository> _agvGraphRepository = new();

    private readonly Guid[] _correlationIds =
    {
        new("7FE9372B-4219-4D09-8885-966EE125A350"),
        new("041AD79E-1952-4AB7-BD3E-9EC972E27FBF"),
        new("465A85A6-6A02-4068-A7AA-E3B4B9821019")
    };

    protected override MapController GetController()
    {
        IRequestClient<GetMapImage> getMapImageRequestClient = GetRequestClient<GetMapImage>();
        IRequestClient<GetMap> getMapRequestClient = GetRequestClient<GetMap>();
        IRequestClient<GetMapGraph> getMapGraphClient = GetRequestClient<GetMapGraph>();

        var controller = new MapController(GetMapper(), getMapImageRequestClient, getMapRequestClient, getMapGraphClient);

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

    /// <summary>
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="serviceAction">The action of the service to add</param>
    /// <returns></returns>
    [UsedImplicitly]
    public MapControllerTestFixture<TRequest, TRequestHandler> AddService(Action<IServiceCollection> serviceAction)
    {
        AddServices(serviceAction);
        return this;
    }

    /// <summary>
    /// Adds the <see cref="IMapRepository"/> mock object
    /// </summary>
    /// <returns></returns>
    [UsedImplicitly]
    public MapControllerTestFixture<TRequest, TRequestHandler> AddMapRepositoryMock(Action<Mock<IMapRepository>> mapRepositoryAction)
    {
        var mapRepositoryMock = new Mock<IMapRepository>();
        mapRepositoryAction.Invoke(mapRepositoryMock);

        AddServices(sc => sc.AddTransient(_ => mapRepositoryMock.Object));

        return this;
    }

    /// <summary>
    /// Adds the <see cref="IMapRepository"/> mock object
    /// </summary>
    /// <returns></returns>
    public MapControllerTestFixture<TRequest, TRequestHandler> AddMapRepositoryMock()
    {
        AddServices(sc => sc.AddTransient(_ => GetMapRepository().Object));

        return this;
    }

    public new MapControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();
        return this;
    }
}