using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Map;

/// <summary>
/// Testing fixture to test map request
/// </summary>
/// <typeparam name="TRequestClient"></typeparam>
/// <typeparam name="TConsumer"></typeparam>
public class MapRequestTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    public const string Base64String = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private readonly string[] _agvIds = {"AGV001", "AGV002"};
    private readonly string[] _orders = {"ORDER001", "ORDER002", ""};
    private readonly AgvState[] _states = {AgvState.Busy, AgvState.Discovered};

    private readonly GraphNode[] _nodes =
    {
        new("node00") {X = 0f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node10") {X = 1f, Y = 0f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node01") {X = 0f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint},
        new("node11") {X = 1f, Y = 1f, Z = 0f, Type = AgvGraph.NodeType.WayPoint}
    };

    private readonly Guid[] _correlationIds =
    {
        new("7FE9372B-4219-4D09-8885-966EE125A350"),
        new("041AD79E-1952-4AB7-BD3E-9EC972E27FBF"),
        new("465A85A6-6A02-4068-A7AA-E3B4B9821019")
    };

    private readonly GraphEdge[] _edges =
    {
        new("node00", "node01", 1f),
        new("node01", "node11", 1f),
        new("node11", "node10", 1f),
        new("node10", "node00", 1f)
    };

    /// <summary>
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="serviceAction">The action of the service to add</param>
    /// <returns></returns>
    [UsedImplicitly]
    public MapRequestTestFixture<TRequestClient, TConsumer> AddService(Action<IServiceCollection> serviceAction)
    {
        AddServices(serviceAction);
        return this;
    }

    /// <summary>
    /// Uses the default services
    /// </summary>
    /// <returns></returns>
    public MapRequestTestFixture<TRequestClient, TConsumer> UseDefaultServices()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => GetMapRegistry().Object);
            sc.AddTransient(_ => GetAgvRegistry().Object);
            sc.AddTransient(_ => GetMapper());
            sc.AddTransient(_ => GetAgvGraphRegistry().Object);
        });
        return this;
    }

    /// <summary>
    /// Uses the default services
    /// </summary>
    /// <returns></returns>
    public MapRequestTestFixture<TRequestClient, TConsumer> AddMapRegistryMock(Action<Mock<IMapRegistry>> mapRegistryAction)
    {
        var mapRegistryMock = new Mock<IMapRegistry>();
        mapRegistryAction.Invoke(mapRegistryMock);

        AddServices(sc => sc.AddTransient(_ => mapRegistryMock.Object));

        return this;
    }

    public new MapRequestTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }

    private Mock<IMapRegistry> GetMapRegistry()
    {
        var mapRegistryMock = new Mock<IMapRegistry>();

        mapRegistryMock.Setup(x => x.GetAllMaps()).Returns(new EnumerableQuery<TransportOrder.Domain.Models.Map.Map>(new List<TransportOrder.Domain.Models.Map.Map>
        {
            new(_correlationIds[0], "Area51_Floor1_Compartment47", Convert.FromBase64String(Base64String), 1, 1, 0.01, 1, 2, DateTime.UtcNow),
            new(_correlationIds[1], "Area51_Floor1_Compartment48", Convert.FromBase64String(Base64String), 1, 1, 0.02, 2, 3, DateTime.UtcNow),
            new(_correlationIds[2], "Area51_Floor1_Compartment49", Convert.FromBase64String(Base64String), 1, 1, 0.03, 2, 1, DateTime.UtcNow)
        }));

        return mapRegistryMock;
    }

    private Mock<IAgvRepository> GetAgvRegistry()
    {
        var registryMock = new Mock<IAgvRepository>();

        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv>();

        for (int i = 0; i < _agvIds.Length; i++)
        {
            string agvId = _agvIds[i];
            string order = _orders[i];
            var state = _states[i];
            var correlationId = _correlationIds[i];
            var agvStatus = new TransportOrder.Domain.Models.Agv.AgvStatus(correlationId, order, true, true, 10, true, true);

            var agv = new TransportOrder.Domain.Models.Agv.Agv(correlationId, agvId, agvStatus);
            agv = AgvGenerator.SetAgvState(state, false, agv);

            agvs.Add(agv);
        }

        registryMock.SetupGet(x => x.Count).Returns(agvs.Count);
        registryMock.Setup(x => x.GetIdleAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>(agvs.Where(x => x.CurrentAgvState == AgvState.Idle)));
        registryMock.Setup(x => x.GetAllAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>(agvs));
        registryMock.Setup(x => x.GetAgvAsync(It.IsAny<Guid>())).Returns<Guid>(correlationId => Task.FromResult(agvs.Single(a => a.CorrelationId == correlationId)));

        return registryMock;
    }

    private Mock<IAgvGraphRegistry> GetAgvGraphRegistry()
    {
        var registryMock = new Mock<IAgvGraphRegistry>();
        const string mapName = "Map#1";

        var agvGraphs = new List<AgvGraph>();
        for (int i = 0; i < _agvIds.Length; i++)
        {
            var correlationId = _correlationIds[i];

            agvGraphs.Add(new AgvGraph(correlationId, _nodes, _edges, _correlationIds[2], mapName));
        }

        registryMock.Setup(x => x.GetAllAgvGraphsAsync()).ReturnsAsync(agvGraphs);
        return registryMock;
    }

    private IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(
            new AgvProfile(),
            new AgvGraphProfile(),
            new MapProfile()
        );
        return mapper;
    }
}