using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Core.Services;

public class BoundingBoxConflictRequestHandlerTestFixture
    : MediatorTestFixture<GetBoundingBoxConflicts, BoundingBoxConflictRequestHandler, MockConsumer>
{
    public static readonly Guid NodeId = Guid.NewGuid();
    public static readonly Guid EdgeId = Guid.NewGuid();
    public static readonly Guid OtherResourceId = Guid.NewGuid();
    public static readonly Guid UnknownId = Guid.NewGuid();

    private readonly Mock<IRoutingResourceConflictRepository> _conflictRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly List<ResourceConflict> _conflicts = [];

    public BoundingBoxConflictRequestHandlerTestFixture()
    {
        _agvDb.Setup(x => x.NewGraphs.RoutingNodeExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        _agvDb.Setup(x => x.NewGraphs.RoutingEdgeExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        _conflictRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(() => _conflicts);
        _conflictRepository.Setup(x => x.GetByResourceAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid resourceId) => _conflicts
                .Where(c => c.ResourceId == resourceId || c.ConflictingResourceId == resourceId)
                .ToList());
    }

    protected override IEnumerable<Profile> GetProfiles()
    {
        return [new BoundingBoxProfile()];
    }

    public BoundingBoxConflictRequestHandlerTestFixture WithConflicts(params ResourceConflict[] conflicts)
    {
        _conflicts.AddRange(conflicts);
        return this;
    }

    public BoundingBoxConflictRequestHandlerTestFixture WithNode(Guid nodeId)
    {
        _agvDb.Setup(x => x.NewGraphs.RoutingNodeExistsAsync(nodeId)).ReturnsAsync(true);
        return this;
    }

    public BoundingBoxConflictRequestHandlerTestFixture WithEdge(Guid edgeId)
    {
        _agvDb.Setup(x => x.NewGraphs.RoutingEdgeExistsAsync(edgeId)).ReturnsAsync(true);
        return this;
    }

    public override BoundingBoxConflictRequestHandlerTestFixture Build()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _conflictRepository.Object);
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