using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfGraphRepositoryTest
{
    [TestMethod]
    public async Task ConsolidateGraphAsync_ActiveActionHasStalePathNode_ActionIsAbortedAndNodesCleared()
    {
        var fixture = new EfGraphRepositoryTestFixture();
        var agvId = await fixture.SetupGraphAsync([AgvGraphGenerator.Src1Node, AgvGraphGenerator.Dst1Node]);

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.Src1,
            destination: AgvGraphGenerator.Dst1);
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Nodes = [AgvGraphGenerator.Src1Node];
        await fixture.AddActionGroupAsync(actionGroup);

        await fixture.GraphRepository.AddOrUpdateAgvGraphAsync(new AgvGraph(agvId, [AgvGraphGenerator.Dst1Node], []));

        (await fixture.GraphRepository.ConsolidateGraphAsync()).ShouldBeTrue();

        var loaded = await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId);
        var loadedPick = loaded.Actions.Single(a => a.ActionType == ActionType.Pick);
        loadedPick.State.ShouldBe(ActionState.Aborted);
        loadedPick.AgvActionNodes.ShouldBeEmpty();
        loaded.State.ShouldBe(ActionGroupState.Aborted);
        (await fixture.GraphRepository.NodeExistsByIdAsync(AgvGraphGenerator.Src1Node.Id)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task ConsolidateGraphAsync_ActiveActionHasStaleTargetNode_ActionIsAbortedAndTargetCleared()
    {
        var fixture = new EfGraphRepositoryTestFixture();
        var agvId = await fixture.SetupGraphAsync([AgvGraphGenerator.Src1Node]);

        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: AgvGraphGenerator.Src1);
        await fixture.AddActionGroupAsync(actionGroup);

        await fixture.GraphRepository.AddOrUpdateAgvGraphAsync(new AgvGraph(agvId, [], []));

        (await fixture.GraphRepository.ConsolidateGraphAsync()).ShouldBeTrue();

        var loaded = await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId);
        var loadedMove = loaded.Actions.Single(a => a.ActionType == ActionType.Move);
        loadedMove.State.ShouldBe(ActionState.Aborted);
        loadedMove.Target.NodeId.ShouldBeNull();
        (await fixture.GraphRepository.NodeExistsByIdAsync(AgvGraphGenerator.Src1Node.Id)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task ConsolidateGraphAsync_CompletedActionHasStalePathNode_NotReAbortedButNodesCleared()
    {
        var fixture = new EfGraphRepositoryTestFixture();
        var agvId = await fixture.SetupGraphAsync([AgvGraphGenerator.Src1Node, AgvGraphGenerator.Dst1Node]);

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.Src1,
            destination: AgvGraphGenerator.Dst1);
        actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Nodes = [AgvGraphGenerator.Src1Node];
        AgvActionGenerator.SetActionGroupToState(actionGroup, ActionGroupState.Completed);
        await fixture.AddActionGroupAsync(actionGroup);

        await fixture.GraphRepository.AddOrUpdateAgvGraphAsync(new AgvGraph(agvId, [AgvGraphGenerator.Dst1Node], []));

        (await fixture.GraphRepository.ConsolidateGraphAsync()).ShouldBeTrue();

        var loaded = await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId);
        var loadedPick = loaded.Actions.Single(a => a.ActionType == ActionType.Pick);
        loadedPick.State.ShouldBe(ActionState.Completed);
        loadedPick.AgvActionNodes.ShouldBeEmpty();
        (await fixture.GraphRepository.NodeExistsByIdAsync(AgvGraphGenerator.Src1Node.Id)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task ConsolidateGraphAsync_NoStaleActionReferences_RemovesUnusedNodesSuccessfully()
    {
        var fixture = new EfGraphRepositoryTestFixture();
        var agvId = await fixture.SetupGraphAsync([AgvGraphGenerator.Src1Node, AgvGraphGenerator.Dst1Node]);

        await fixture.GraphRepository.AddOrUpdateAgvGraphAsync(new AgvGraph(agvId, [AgvGraphGenerator.Dst1Node], []));

        (await fixture.GraphRepository.ConsolidateGraphAsync()).ShouldBeTrue();

        (await fixture.GraphRepository.NodeExistsByIdAsync(AgvGraphGenerator.Src1Node.Id)).ShouldBeFalse();
        (await fixture.GraphRepository.NodeExistsByIdAsync(AgvGraphGenerator.Dst1Node.Id)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ConsolidateGraphAsync_DatabaseThrows_ReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<TransportOrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var factoryMock = new Mock<IDbContextFactory<TransportOrderDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ThrowingTransportOrderDbContext(options));

        var repository = new EfGraphRepository(
            factoryMock.Object,
            Mock.Of<IMapper>(),
            new SemaphoreProvider<IGraphRepositorySemaphore>(Mock.Of<ILogger<SemaphoreProvider<IGraphRepositorySemaphore>>>()),
            Mock.Of<ILogger<EfGraphRepository>>(), Mock.Of<ILoggingService>());

        (await repository.ConsolidateGraphAsync()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateAgvTypeGraphsAsync_ActiveActionHasStaleTargetNode_ActionIsAbortedAndTargetCleared()
    {
        var fixture = new EfGraphRepositoryTestFixture();
        var vdaNode1 = new GraphNode(AgvGraphGenerator.Src1Node.Id, AgvGraphGenerator.Src1) {  X = 1f, Y = 1f, Z = 0f };
        var vdaNode2 = new GraphNode(AgvGraphGenerator.Dst1Node.Id, AgvGraphGenerator.Dst1) {  X = 2f, Y = 5f, Z = 0f };

        await fixture.SetupVdaTypeGraphAsync([vdaNode1]);

        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: AgvGraphGenerator.Src1);
        await fixture.ActionGroupRepository.AddAsync(actionGroup);

        await fixture.SetupVdaTypeGraphAsync([vdaNode2]);

        var loaded = await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId);
        var loadedMove = loaded.Actions.Single(a => a.ActionType == ActionType.Move);
        loadedMove.State.ShouldBe(ActionState.Aborted);
        loadedMove.Target.NodeId.ShouldBeNull();
        (await fixture.GraphRepository.NodeExistsByIdAsync(vdaNode1.Id)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateAgvTypeGraphsAsync_ActiveActionHasStalePathNode_ActionIsAbortedAndNodesCleared()
    {
        var fixture = new EfGraphRepositoryTestFixture();
        var vdaNode1 = new GraphNode(AgvGraphGenerator.Src1Node.Id, AgvGraphGenerator.Src1) { X = 1f, Y = 1f, Z = 0f };
        var vdaNode2 = new GraphNode(AgvGraphGenerator.Dst1Node.Id, AgvGraphGenerator.Dst1) {  X = 2f, Y = 5f, Z = 0f };

        await fixture.SetupVdaTypeGraphAsync([vdaNode1]);

        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: AgvGraphGenerator.Src1);
        actionGroup.Actions.Single().Nodes = [vdaNode1];
        await fixture.ActionGroupRepository.AddAsync(actionGroup);

        await fixture.SetupVdaTypeGraphAsync([vdaNode2]);

        var loaded = await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId);
        var loadedMove = loaded.Actions.Single();
        loadedMove.State.ShouldBe(ActionState.Aborted);
        loadedMove.AgvActionNodes.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task UpdateAgvTypeGraphsAsync_CompletedActionHasStaleTargetNode_TargetClearedNotReAborted()
    {
        var fixture = new EfGraphRepositoryTestFixture();
        var vdaNode1 = new GraphNode(AgvGraphGenerator.Src1Node.Id, AgvGraphGenerator.Src1) {  X = 1f, Y = 1f, Z = 0f };
        var vdaNode2 = new GraphNode(AgvGraphGenerator.Dst1Node.Id, AgvGraphGenerator.Dst1) {  X = 2f, Y = 5f, Z = 0f };

        await fixture.SetupVdaTypeGraphAsync([vdaNode1]);

        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: AgvGraphGenerator.Src1);
        AgvActionGenerator.SetActionGroupToState(actionGroup, ActionGroupState.Completed);
        await fixture.ActionGroupRepository.AddAsync(actionGroup);

        await fixture.SetupVdaTypeGraphAsync([vdaNode2]);

        var loaded = await fixture.ActionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroup.CorrelationId);
        var loadedMove = loaded.Actions.Single(a => a.ActionType == ActionType.Move);
        loadedMove.State.ShouldBe(ActionState.Completed);
        loadedMove.Target.NodeId.ShouldBeNull();
        (await fixture.GraphRepository.NodeExistsByIdAsync(vdaNode1.Id)).ShouldBeFalse();
    }

    private sealed class ThrowingTransportOrderDbContext(DbContextOptions<TransportOrderDbContext> options) : TransportOrderDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated DB failure");
    }
}