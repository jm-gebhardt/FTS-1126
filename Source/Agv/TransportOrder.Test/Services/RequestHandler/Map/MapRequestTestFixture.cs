using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

/// <summary>
/// Testing fixture to test map request
/// </summary>
/// <typeparam name="TRequestClient"></typeparam>
/// <typeparam name="TConsumer"></typeparam>
public class MapRequestTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, MockConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    public const string Base64String = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private readonly string[] _agvSerialNumbers = { "AGV001", "AGV002" };
    private readonly string[] _orders = { "ORDER001", "ORDER002", "" };
    private readonly AgvState[] _states = { AgvState.Busy, AgvState.Discovered };

    private readonly Guid[] _correlationIds =
    {
        new("7FE9372B-4219-4D09-8885-966EE125A350"),
        new("041AD79E-1952-4AB7-BD3E-9EC972E27FBF"),
        new("465A85A6-6A02-4068-A7AA-E3B4B9821019")
    };

    public Mock<IMapRepository> MapRepositoryMock { get; }

    public List<TransportOrder.Domain.Models.Map.Map> Maps { get; }

    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new AgvProfile(),
        new AgvGraphProfile(),
        new MapProfile()
    ];

    public MapRequestTestFixture()
    {
        Maps = new List<TransportOrder.Domain.Models.Map.Map>
        {
            new(_correlationIds[0], "Area51_Floor1_Compartment47", Convert.FromBase64String(Base64String), 1, 1, 0.01, 1, 2, DateTime.UtcNow),
            new(_correlationIds[1], "Area51_Floor1_Compartment48", Convert.FromBase64String(Base64String), 1, 1, 0.02, 2, 3, DateTime.UtcNow),
            new(_correlationIds[2], "Area51_Floor1_Compartment49", Convert.FromBase64String(Base64String), 1, 1, 0.03, 2, 1, DateTime.UtcNow)
        };
        MapRepositoryMock = GetMapRepository();
        AddServices(sc =>
        {
            sc.AddTransient(_ => MapRepositoryMock.Object);
            sc.AddTransient(_ => GetAgvRepository().Object);
        });

        base.Build();
    }

    private Mock<IMapRepository> GetMapRepository()
    {
        var mapRepositoryMock = new Mock<IMapRepository>();

        mapRepositoryMock.Setup(x => x.GetMapAsync(It.IsAny<Guid>())).ReturnsAsync((Guid correlationId) => Maps.Single(m => m.CorrelationId == correlationId));
        mapRepositoryMock.Setup(x => x.GetAllMapsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Map.Map>(Maps));

        return mapRepositoryMock;
    }

    private Mock<IAgvRepository> GetAgvRepository()
    {
        var repositoryMock = new Mock<IAgvRepository>();

        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv>();

        for (int i = 0; i < _agvSerialNumbers.Length; i++)
        {
            string agvSerialNumber = _agvSerialNumbers[i];
            string order = _orders[i];
            var state = _states[i];
            var correlationId = _correlationIds[i];
            var agvStatus = new AgvStatus(correlationId, order, true, true, 10, true, true);

            var agv = AgvGenerator.GenerateRawAgv(correlationId, agvSerialNumber, agvStatus);
            agv = AgvGenerator.SetAgvState(state, agv);

            agvs.Add(agv);
        }

        repositoryMock.AddAgvs(agvs.ToArray());
        return repositoryMock;
    }

    public MapRequestTestFixture<TRequestClient, TConsumer> WithImage(string? image)
    {
        MapRepositoryMock.Setup(x => x.GetMapImage(It.IsAny<Guid>())).Returns(image != null ? Convert.FromBase64String(image) : null);
        return this;
    }
}