using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Repositories;

[TestClass]
public class HostOrderRepositoryTest
{
    [TestMethod]
    public async Task GetLatestUnfinishedOrdersByHu_ValidData_ReturnsUnfinishedOrders()
    {
        const string hu = "hu";
        var requestDate = DateTime.UtcNow;
        var createdDate = requestDate.AddMinutes(-2);
        var finishDate = requestDate.AddMinutes(-1);

        var unfinishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(hu), createdDate);
        var anotherUnfinishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(hu), createdDate.AddMinutes(-2));
        var orderCreatedAtRequestTime = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(hu), requestDate);
        var finishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(hu), createdDate.AddMinutes(-3), finishDate);
        var unfinishedOrderForDifferentHu = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder("another hu"), createdDate);

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(unfinishedOrder, anotherUnfinishedOrder, finishedOrder, unfinishedOrderForDifferentHu, orderCreatedAtRequestTime);
        await fixture.Repository.SaveAsync();

        ICollection<HostOrder> hostOrders = await fixture.Repository.GetLatestUnfinishedOrdersByHu(hu, requestDate);
        hostOrders.Count.ShouldBe(2);
        hostOrders.Select(h => h.OrderId).ShouldBeSubsetOf([unfinishedOrder.OrderId, anotherUnfinishedOrder.OrderId]);
    }

    [TestMethod]
    public async Task GetLatestUnfinishedOrderByHu_ValidData_ReturnsUnfinishedOrders()
    {
        const string hu = "hu";
        var now = DateTime.UtcNow;
        var createdDate = now.AddMinutes(-2);
        var finishDate = now.AddMinutes(-1);

        var unfinishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(hu), createdDate);
        var anotherUnfinishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(hu), createdDate.AddMinutes(-2));
        var finishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(hu), createdDate.AddMinutes(-3), finishDate);
        var unfinishedOrderForDifferentHu = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder("another hu"), createdDate);

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(unfinishedOrder, anotherUnfinishedOrder, finishedOrder, unfinishedOrderForDifferentHu);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetLatestUnfinishedOrderByHu(hu)).ShouldBeEquivalentTo(unfinishedOrder);
    }

    [TestMethod]
    public async Task GetNextDropOrderEligibleForDoppelspiel_ValidData_DoppelspielPartnerReturned()
    {
        var doppelspielPartnerCandidate = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        doppelspielPartnerCandidate.Start(DateTime.UtcNow, Guid.NewGuid());
        doppelspielPartnerCandidate.FinishAction(DateTime.UtcNow, ActionType.Pick);
        doppelspielPartnerCandidate.IsQueued = true;
        doppelspielPartnerCandidate.ConsiderForDoppelspiel();

        var unfinishedPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        unfinishedPickOrder.IsQueued = true;
        unfinishedPickOrder.Start(DateTime.UtcNow, Guid.NewGuid());

        var notStartedOrderPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));

        var notConsideredForDoppelspielOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        notConsideredForDoppelspielOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        notConsideredForDoppelspielOrder.FinishAction(DateTime.UtcNow, ActionType.Pick);
        notConsideredForDoppelspielOrder.IsQueued = true;

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(doppelspielPartnerCandidate, notStartedOrderPickOrder, unfinishedPickOrder, notConsideredForDoppelspielOrder);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextDropOrderEligibleForDoppelspiel(doppelspielPartnerCandidate.Target)).ShouldBe(doppelspielPartnerCandidate.CorrelationId);
    }

    [TestMethod]
    public async Task HasHostOrdersForStationGroup_RunningOrderExists_ReturnsTrue()
    {
        var fixture = new HostOrderRepositoryTestFixture();
        var runningOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        runningOrder.IsQueued = true;
        runningOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        fixture.Repository.Add(runningOrder);
        await fixture.Repository.SaveAsync();
        bool hasHostOrdersForStationGroup = await fixture.Repository.HasHostOrdersForStationGroup([runningOrder.Source], [runningOrder.Target], null);
        hasHostOrdersForStationGroup.ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasHostOrdersForStationGroup_QueuedButNotStartedOrderExists_ReturnsTrue()
    {
        var fixture = new HostOrderRepositoryTestFixture();
        var runningOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        runningOrder.IsQueued = true;
        fixture.Repository.Add(runningOrder);
        await fixture.Repository.SaveAsync();
        bool hasHostOrdersForStationGroup = await fixture.Repository.HasHostOrdersForStationGroup([runningOrder.Source], [runningOrder.Target], null);
        hasHostOrdersForStationGroup.ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasHostOrdersForStationGroup_RunningOrderButSameAgv_ReturnsFalse()
    {
        var fixture = new HostOrderRepositoryTestFixture();
        var runningOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        runningOrder.IsQueued = true;
        var agvCorrelationId = Guid.NewGuid();
        runningOrder.Start(DateTime.UtcNow, agvCorrelationId);
        fixture.Repository.Add(runningOrder);
        await fixture.Repository.SaveAsync();
        bool hasHostOrdersForStationGroup = await fixture.Repository.HasHostOrdersForStationGroup([runningOrder.Source], [runningOrder.Target], agvCorrelationId);
        hasHostOrdersForStationGroup.ShouldBeFalse();
    }

    [TestMethod]
    public async Task HasHostOrdersForStationGroup_RunningOrderButOtherAgv_ReturnsTrue()
    {
        var fixture = new HostOrderRepositoryTestFixture();
        var runningOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        runningOrder.IsQueued = true;
        var agvCorrelationId = Guid.NewGuid();
        runningOrder.Start(DateTime.UtcNow, agvCorrelationId);
        fixture.Repository.Add(runningOrder);
        await fixture.Repository.SaveAsync();
        bool hasHostOrdersForStationGroup = await fixture.Repository.HasHostOrdersForStationGroup([runningOrder.Source], [runningOrder.Target], Guid.NewGuid());
        hasHostOrdersForStationGroup.ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasHostOrdersForStationGroup_RunningOrderButNotAtThatStartStationExists_ReturnsTrue()
    {
        var fixture = new HostOrderRepositoryTestFixture();
        var runningOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        runningOrder.IsQueued = true;
        fixture.Repository.Add(runningOrder);
        await fixture.Repository.SaveAsync();
        bool hasHostOrdersForStationGroup = await fixture.Repository.HasHostOrdersForStationGroup(["some-other-source"], [runningOrder.Target], null);
        hasHostOrdersForStationGroup.ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasHostOrdersForStationGroup_RunningOrderButNotAtThatTargetStationExists_ReturnsTrue()
    {
        var fixture = new HostOrderRepositoryTestFixture();
        var runningOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        runningOrder.IsQueued = true;
        fixture.Repository.Add(runningOrder);
        await fixture.Repository.SaveAsync();
        bool hasHostOrdersForStationGroup = await fixture.Repository.HasHostOrdersForStationGroup([runningOrder.Source], ["some-other-target"], null);
        hasHostOrdersForStationGroup.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNextPickForSources_OrderRegisteredForDoppelspiel_ReturnsRegisteredNextPick()
    {
        var createdDate = DateTime.UtcNow;
        var nextUnqueuedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(), createdDate: createdDate);
        var nextQueuedPick = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(), createdDate: createdDate);
        nextQueuedPick.IsQueued = true;
        var drop = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        var startedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        startedPickAtSource.Start(DateTime.UtcNow, Guid.NewGuid());
        var finishedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        finishedPickAtSource.Start(DateTime.UtcNow, Guid.NewGuid());
        finishedPickAtSource.Finish(DateTime.UtcNow);

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(nextUnqueuedPickAtSource, nextQueuedPick, drop, startedPickAtSource, finishedPickAtSource);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextPickForSources([nextQueuedPick.Source]))?.OrderId.ShouldBe(nextUnqueuedPickAtSource.OrderId);
    }

    [TestMethod]
    public async Task GetNextPickForSources_ValidData_ReturnsNextPick()
    {
        var createdDate = DateTime.UtcNow;
        var nextPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(), createdDate: createdDate);
        var drop = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        var startedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        startedPickAtSource.Start(DateTime.UtcNow, Guid.NewGuid());
        var finishedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        finishedPickAtSource.Start(DateTime.UtcNow, Guid.NewGuid());
        finishedPickAtSource.Finish(DateTime.UtcNow);

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(nextPickAtSource, drop, startedPickAtSource, finishedPickAtSource);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextPickForSources([nextPickAtSource.Source]))?.OrderId.ShouldBe(nextPickAtSource.OrderId);
    }

    [TestMethod]
    public async Task GetNextPickForSources_NoNextPick_ReturnsNull()
    {
        var drop = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        var startedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        startedPickAtSource.Start(DateTime.UtcNow, Guid.NewGuid());
        var finishedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        finishedPickAtSource.Start(DateTime.UtcNow, Guid.NewGuid());
        finishedPickAtSource.Finish(DateTime.UtcNow);

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(drop, startedPickAtSource, finishedPickAtSource);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextPickForSources([startedPickAtSource.Source])).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNextUnmatchedDoppelspielPick_AllPicksAreMatched_ReturnsNull()
    {
        var pickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        pickAtSource.RegisterDoppelspiel(ActionType.Pick);
        var dropAtTarget = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        dropAtTarget.ConsiderForDoppelspiel();
        dropAtTarget.RegisterDoppelspiel(ActionType.Drop);
        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(pickAtSource, dropAtTarget);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextUnmatchedDoppelspielPick([pickAtSource.Source], [dropAtTarget.Target], Guid.NewGuid())).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNextUnmatchedDoppelspielPick_NoPick_ReturnsNull()
    {
        var pickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        var dropAtTarget = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        dropAtTarget.ConsiderForDoppelspiel();
        dropAtTarget.RegisterDoppelspiel(ActionType.Drop);
        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(dropAtTarget);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextUnmatchedDoppelspielPick([pickAtSource.Source], [dropAtTarget.Target], Guid.NewGuid())).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNextUnmatchedDoppelspielPick_UnmatchedPick_ReturnsPickOrder()
    {
        var pickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        pickAtSource.RegisterDoppelspiel(ActionType.Pick);
        var dropAtTarget = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        dropAtTarget.Start(DateTime.UtcNow, Guid.NewGuid());
        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(pickAtSource, dropAtTarget);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextUnmatchedDoppelspielPick([pickAtSource.Source], [dropAtTarget.Target], dropAtTarget.AssignedAgv!.Value))?.OrderId.ShouldBe(pickAtSource.OrderId);
    }

    [TestMethod]
    public async Task GetCurrentHostOrderForAgv_ValidData_ReturnsHostOrder()
    {
        var agv = Guid.NewGuid();
        var createdUnassignedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        createdUnassignedOrder.IsQueued = true;

        var abortedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        abortedOrder.IsQueued = true;
        abortedOrder.Start(DateTime.UtcNow, agv);
        abortedOrder.HandleFailure(DateTime.UtcNow);

        var finishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        finishedOrder.IsQueued = true;
        finishedOrder.Start(DateTime.UtcNow, agv);
        finishedOrder.Finish(DateTime.UtcNow);

        var startedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        startedOrder.IsQueued = true;
        startedOrder.Start(DateTime.UtcNow, agv);

        var startedOrderDifferentAgv = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        startedOrderDifferentAgv.IsQueued = true;
        startedOrderDifferentAgv.Start(DateTime.UtcNow, Guid.NewGuid());

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(createdUnassignedOrder, abortedOrder, finishedOrder, startedOrder, startedOrderDifferentAgv);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetCurrentHostOrderForAgv(agv))?.CorrelationId.ShouldBe(startedOrder.CorrelationId);
    }

    [TestMethod]
    public async Task GetCurrentHostOrderForAgv_NoStartedOrderForAgv_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var createdUnassignedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        createdUnassignedOrder.IsQueued = true;

        var abortedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        abortedOrder.IsQueued = true;
        abortedOrder.Start(DateTime.UtcNow, agv);
        abortedOrder.HandleFailure(DateTime.UtcNow);

        var finishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        finishedOrder.IsQueued = true;
        finishedOrder.Start(DateTime.UtcNow, agv);
        finishedOrder.Finish(DateTime.UtcNow);

        var startedOrderDifferentAgv = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        startedOrderDifferentAgv.IsQueued = true;
        startedOrderDifferentAgv.Start(DateTime.UtcNow, Guid.NewGuid());

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(createdUnassignedOrder, abortedOrder, finishedOrder, startedOrderDifferentAgv);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetCurrentHostOrderForAgv(agv)).ShouldBeNull();
    }
}