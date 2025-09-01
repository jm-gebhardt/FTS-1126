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
        hostOrders.ShouldBeSubsetOf([unfinishedOrder, anotherUnfinishedOrder]);
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
    public async Task GetNextDropOrderHeadingTowardsDoppelspielTriggerPoint_ValidData_DoppelspielPartnerReturned()
    {
        var doppelspielPartnerCandidate = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        doppelspielPartnerCandidate.Start(DateTime.UtcNow, Guid.NewGuid());
        doppelspielPartnerCandidate.FinishAction(DateTime.UtcNow, ActionType.Pick);
        doppelspielPartnerCandidate.IsQueued = true;

        var unfinishedPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        unfinishedPickOrder.IsQueued = true;
        unfinishedPickOrder.Start(DateTime.UtcNow, Guid.NewGuid());

        var notStartedOrderPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));

        var passedTriggerPointOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        passedTriggerPointOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        passedTriggerPointOrder.FinishAction(DateTime.UtcNow, ActionType.Pick);
        passedTriggerPointOrder.IsQueued = true;
        passedTriggerPointOrder.PassTriggerPoint();

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(doppelspielPartnerCandidate, notStartedOrderPickOrder, unfinishedPickOrder, passedTriggerPointOrder);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextDropOrderHeadingTowardsDoppelspielTriggerPoint(doppelspielPartnerCandidate.Target)).ShouldBe(doppelspielPartnerCandidate.CorrelationId);
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
        var nextPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(), createdDate: createdDate);
        var nextPickAtSourceRegisteredForDoppelspiel = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(), createdDate: createdDate);
        nextPickAtSourceRegisteredForDoppelspiel.RegisterForDoppelspiel();
        var drop = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        var startedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        startedPickAtSource.Start(DateTime.UtcNow, Guid.NewGuid());
        var finishedPickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        finishedPickAtSource.Start(DateTime.UtcNow, Guid.NewGuid());
        finishedPickAtSource.Finish(DateTime.UtcNow);

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(nextPickAtSource, nextPickAtSourceRegisteredForDoppelspiel, drop, startedPickAtSource, finishedPickAtSource);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextPickForSources([nextPickAtSourceRegisteredForDoppelspiel.Source])).ShouldBe(nextPickAtSourceRegisteredForDoppelspiel);
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
        (await fixture.Repository.GetNextPickForSources([nextPickAtSource.Source])).ShouldBe(nextPickAtSource);
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
        var dropAtTarget = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
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
        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(dropAtTarget);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextUnmatchedDoppelspielPick([pickAtSource.Source], [dropAtTarget.Target], Guid.NewGuid())).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNextUnmatchedDoppelspielPick_UnmatchedPick_ReturnsPickOrder()
    {
        var pickAtSource = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        var dropAtTarget = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        dropAtTarget.Start(DateTime.UtcNow, Guid.NewGuid());
        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(pickAtSource, dropAtTarget);
        await fixture.Repository.SaveAsync();
        (await fixture.Repository.GetNextUnmatchedDoppelspielPick([pickAtSource.Source], [dropAtTarget.Target], dropAtTarget.AssignedAgv!.Value)).ShouldBe(pickAtSource);
    }
}