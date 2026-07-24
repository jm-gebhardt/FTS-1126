using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Agv = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Agv;

namespace Gebhardt.Acc.TransportOrder.Test.Core.Services;

public class BoundingBoxConflictRequestHandlerTestFixture
    : MediatorTestFixture<GetBoundingBoxConflicts, BoundingBoxConflictRequestHandler, MockConsumer>
{
    private readonly Mock<IBoundingBoxRepository> _boundingBoxRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    public BoundingBoxConflictRequestHandlerTestFixture()
    {
        _agvDb.Setup(x => x.Graphs.GetGraphAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Graph());
        _agvDb.Setup(x => x.Agvs.GetAllAgvsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Agv>());
        _agvDb.Setup(x => x.Data.GetSelectedAgvDataAsync(It.IsAny<ICollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AgvData>());
        _boundingBoxRepository.Setup(x => x.GetByAgvTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ResourceBoundingBox>());
    }

    public BoundingBoxConflictRequestHandlerTestFixture WithGraph(Graph graph)
    {
        _agvDb.Setup(x => x.Graphs.GetGraphAsync(It.IsAny<CancellationToken>())).ReturnsAsync(graph);
        return this;
    }

    public BoundingBoxConflictRequestHandlerTestFixture WithAgvData(params AgvData[] agvDatas)
    {
        Agv[] agvs = agvDatas.Select(d => AgvGenerator.GenerateRawAgv(d.AgvCorrelationId, "")).ToArray();
        _agvDb.Setup(x => x.Agvs.GetAllAgvsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(agvs);
        _agvDb.Setup(x => x.Data.GetSelectedAgvDataAsync(It.IsAny<ICollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agvDatas);
        return this;
    }

    public BoundingBoxConflictRequestHandlerTestFixture WithBoxesByAgvType(string agvType, params ResourceBoundingBox[] boxes)
    {
        _boundingBoxRepository.Setup(x => x.GetByAgvTypeAsync(agvType, It.IsAny<CancellationToken>())).ReturnsAsync(boxes);
        return this;
    }

    public override BoundingBoxConflictRequestHandlerTestFixture Build()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _boundingBoxRepository.Object);
            s.AddTransient(_ => _agvDb.Object);
        });

        base.Build();
        return this;
    }

    public Task<Response<GotBoundingBoxConflicts>> RequestConflictsAsync<TRequest>(TRequest request) where TRequest : class
    {
        return Mediator!.CreateRequestClient<TRequest>().GetResponse<GotBoundingBoxConflicts>(request);
    }
}