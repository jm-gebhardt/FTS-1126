using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Stations.Requests;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

public class StationTypeRequestHandlerTestFixture : MassTransitConsumerTestFixture<StationTypeRequestHandler>
{
    public const string StationName = "station";
    public const AgvGraph.NodeType PrimaryType = AgvGraph.NodeType.Pick;
    public const AgvGraph.NodeType AdditionalType = AgvGraph.NodeType.Drop;

    private readonly Mock<IStationRepository> _stationRepository = new();

    public static Station GenerateStation(string name = StationName, AgvGraph.NodeType primaryType = PrimaryType, params AgvGraph.NodeType[] additionalTypes)
    {
        return new Station(name, primaryType, additionalTypes);
    }

    public StationTypeRequestHandlerTestFixture WithStations(params Station[] stations)
    {
        _stationRepository.Setup(x => x.GetStations(It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(stations);
        return this;
    }

    public new StationTypeRequestHandlerTestFixture Build()
    {
        AddServices(sc => sc.AddTransient(_ => _stationRepository.Object));
        base.Build();
        return this;
    }

    public async Task<Response<GotStationTypes>> GetResponseAsync(GetStationTypes request)
    {
        var client = TestHarness.Bus.CreateRequestClient<GetStationTypes>();
        return await client.GetResponse<GotStationTypes>(request);
    }
}