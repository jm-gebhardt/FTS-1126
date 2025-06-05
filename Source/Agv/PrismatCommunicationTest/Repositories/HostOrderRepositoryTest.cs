using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        var doppelspielPartnerCandidate = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020:false));
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