using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Messages.Extensions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using MassTransit;
using Shouldly;
using Fixture = Gebhardt.Acc.TransportOrder.Test.Core.Services.BoundingBoxConflictRequestHandlerTestFixture;

namespace Gebhardt.Acc.TransportOrder.Test.Core.Services;

[TestClass]
public class BoundingBoxConflictRequestHandlerTests
{
    [TestMethod]
    public async Task Consume_NoPersistedConflicts_ReturnsEmpty()
    {
        var fixture = new Fixture().Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.GetResponseAsync<GotBoundingBoxConflicts>(new GetBoundingBoxConflicts(Guid.NewGuid()));

        response.Message.Conflicts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_PersistedConflicts_ReturnsAllPairs()
    {
        var fixture = new Fixture()
            .WithConflicts(
                new ResourceConflict(Fixture.NodeId, Fixture.OtherResourceId),
                new ResourceConflict(Fixture.OtherResourceId, Fixture.EdgeId))
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.GetResponseAsync<GotBoundingBoxConflicts>(new GetBoundingBoxConflicts(Guid.NewGuid()));

        response.Message.Conflicts.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task Consume_NodeRequest_ReturnsOnlyConflictsInvolvingThatNode()
    {
        var fixture = new Fixture()
            .WithNode(Fixture.NodeId)
            .WithConflicts(
                new ResourceConflict(Fixture.NodeId, Fixture.OtherResourceId),
                new ResourceConflict(Fixture.EdgeId, Fixture.OtherResourceId))
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), Fixture.NodeId));

        response.Message.Conflicts.ConflictingResourceIds(Fixture.NodeId).ShouldBe([Fixture.OtherResourceId]);
    }

    [TestMethod]
    public async Task Consume_NodeRequest_ConflictStoredOnTheOtherSide_ReturnsPartner()
    {
        var fixture = new Fixture()
            .WithNode(Fixture.NodeId)
            .WithConflicts(new ResourceConflict(Fixture.OtherResourceId, Fixture.NodeId))
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), Fixture.NodeId));

        response.Message.Conflicts.ConflictingResourceIds(Fixture.NodeId).ShouldBe([Fixture.OtherResourceId]);
    }

    [TestMethod]
    public async Task Consume_NodeRequest_NoConflictsForThatNode_ReturnsEmpty()
    {
        var fixture = new Fixture()
            .WithNode(Fixture.NodeId)
            .WithConflicts(new ResourceConflict(Fixture.EdgeId, Fixture.OtherResourceId))
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), Fixture.NodeId));

        response.Message.Conflicts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_NodeRequest_UnknownNode_Throws()
    {
        var fixture = new Fixture().WithNode(Fixture.NodeId).Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(() => fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), Fixture.UnknownId)));
    }

    [TestMethod]
    public async Task Consume_NodeRequest_WithEdgeId_Throws()
    {
        var fixture = new Fixture()
            .WithNode(Fixture.NodeId)
            .WithEdge(Fixture.EdgeId)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<NodeNotFoundException>(() => fixture.RequestConflictsAsync(new GetNodeBoundingBoxConflicts(Guid.NewGuid(), Fixture.EdgeId)));
    }

    [TestMethod]
    public async Task Consume_EdgeRequest_ReturnsOnlyConflictsInvolvingThatEdge()
    {
        var fixture = new Fixture()
            .WithEdge(Fixture.EdgeId)
            .WithConflicts(
                new ResourceConflict(Fixture.EdgeId, Fixture.OtherResourceId),
                new ResourceConflict(Fixture.NodeId, Fixture.OtherResourceId))
            .Build();

        Response<GotBoundingBoxConflicts> response =
            await fixture.RequestConflictsAsync(new GetEdgeBoundingBoxConflicts(Guid.NewGuid(), Fixture.EdgeId));

        response.Message.Conflicts.ConflictingResourceIds(Fixture.EdgeId).ShouldBe([Fixture.OtherResourceId]);
    }

    [TestMethod]
    public async Task Consume_EdgeRequest_WithNodeId_Throws()
    {
        var fixture = new Fixture()
            .WithNode(Fixture.NodeId)
            .WithEdge(Fixture.EdgeId)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<EdgeNotFoundException>(() =>
            fixture.RequestConflictsAsync(new GetEdgeBoundingBoxConflicts(Guid.NewGuid(), Fixture.NodeId)));
    }
}