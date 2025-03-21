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
    public async Task HasDropOrdersHeadingTowardsDecisionPoint_ValidData_ReturnsTrue()
    {
        var fixture = new HostOrderRepositoryTestFixture();

        var order = OrderGenerator.GenerateUnknownDropTargetOrder();
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(order);
        dropHostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        await fixture.Repository.AddAsync(dropHostOrder);

        (await fixture.Repository.HasDropOrdersHeadingTowardsDecisionPoint()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasDropOrdersHeadingTowardsLoadTriggerPoint_ValidData_ReturnsTrue()
    {
        var fixture = new HostOrderRepositoryTestFixture();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder();
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(order);
        dropHostOrder.Start(DateTime.UtcNow, Guid.NewGuid());

        await fixture.Repository.AddAsync(dropHostOrder);

        (await fixture.Repository.HasDropOrdersHeadingTowardsCargoTriggerPoint()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNextIncompleteOrderWithUnknownPickTarget_ValidData_ReturnsOrder()
    {
        var fixture = new HostOrderRepositoryTestFixture();

        var earlierOrder = OrderGenerator.GenerateUnknownPickTargetOrder();
        var earlierOrderWithAssignedAgv = OrderGenerator.GenerateHostOrderFromOrderDetails(earlierOrder);
        earlierOrderWithAssignedAgv.CreatePickDropActionGroupForAgv("a node", Guid.NewGuid(), OrderGenerator.KaeserConfiguration);

        var order = OrderGenerator.GenerateUnknownPickTargetOrder();
        var nextOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(order);

        var anotherOrderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var anotherOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(anotherOrderDetails);

        var startedOrderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var startedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(startedOrderDetails);
        startedOrder.Start(DateTime.UtcNow, Guid.NewGuid());

        fixture.Repository.AddRange(earlierOrderWithAssignedAgv, nextOrder, startedOrder, anotherOrder);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextIncompleteOrderWithUnknownPickTarget()).ShouldBeEquivalentTo(nextOrder);
    }

    [TestMethod]
    public async Task GetNextIncompleteOrderWithUnknownDropTarget_ValidData_ReturnsOrder()
    {
        var fixture = new HostOrderRepositoryTestFixture();

        var order = OrderGenerator.GenerateUnknownDropTargetOrder();
        var nextOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(order);

        var anotherOrderDetails = OrderGenerator.GenerateUnknownDropTargetOrder();
        var anotherOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(anotherOrderDetails);

        var startedOrderDetails = OrderGenerator.GenerateUnknownDropTargetOrder();
        var startedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(startedOrderDetails);
        startedOrder.Start(DateTime.UtcNow, Guid.NewGuid());

        fixture.Repository.AddRange(nextOrder, startedOrder, anotherOrder);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextIncompleteOrderWithUnknownDropTarget()).ShouldBeEquivalentTo(nextOrder);
    }

    [TestMethod]
    public async Task GetNextPickOrderForShortLoopDualOperation_ValidData_ReturnsOrder()
    {
        var earlierOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var laterOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var dropAt1050 = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        var longLoopOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(earlierOrder, laterOrder, dropAt1050, longLoopOrder);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextPickOrderForShortLoopDualOperation()).ShouldBe(earlierOrder);
    }

    [TestMethod]
    public async Task GetNextPickOrderForShortLoopDualOperation_NoUnstartedPick_ReturnsNull()
    {
        var longLoopOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        var startedPickAt1020 = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        startedPickAt1020.Start(DateTime.UtcNow, Guid.NewGuid());
        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(longLoopOrder, startedPickAt1020);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextPickOrderForShortLoopDualOperation()).ShouldBeNull();
    }

    [TestMethod]
    public async Task HasDropOrdersHeadingTowardsShortLoopTriggerPoint_ValidData_ReturnsTrue()
    {
        var earlierOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        earlierOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        var pickAt1020 = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        pickAt1020.Start(DateTime.UtcNow, Guid.NewGuid());
        var longLoopOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        longLoopOrder.Start(DateTime.UtcNow, Guid.NewGuid());

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(earlierOrder, pickAt1020, longLoopOrder);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.HasDropOrdersHeadingTowardsShortLoopTriggerPoint()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasDropOrdersHeadingTowardsShortLoopTriggerPoint_NoOrderDroppingAt1050_ReturnsFalse()
    {
        var pickAt1020 = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        pickAt1020.Start(DateTime.UtcNow, Guid.NewGuid());
        var longLoopOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        longLoopOrder.Start(DateTime.UtcNow, Guid.NewGuid());

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(pickAt1020, longLoopOrder);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.HasDropOrdersHeadingTowardsShortLoopTriggerPoint()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task HasDropOrdersHeadingTowardsShortLoopTriggerPoint_NoStartedOrderThatDidNotPassShortLoopTriggerPoint_ReturnsFalse()
    {
        var passedTriggerPoint = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        passedTriggerPoint.Start(DateTime.UtcNow, Guid.NewGuid());
        passedTriggerPoint.PassTriggerPoint();

        var notStarted = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(passedTriggerPoint, notStarted);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.HasDropOrdersHeadingTowardsShortLoopTriggerPoint()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetNextNotQueuedHostOrderPickingAt_ShortLoop_ReturnsHostOrder()
    {
        var pickAt1020 = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var longLoopOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        var longLoopPick = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(pickAt1020, longLoopOrder, longLoopPick);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextNotQueuedHostOrderPickingAt("1020")).ShouldBeEquivalentTo(pickAt1020);
    }

    [TestMethod]
    public async Task GetNextNotQueuedHostOrderPickingAt_LongLoop_ReturnsHostOrder()
    {
        var pickAt1020 = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var longLoopOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        var longLoopPick = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(pickAt1020, longLoopOrder, longLoopPick);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextNotQueuedHostOrderPickingAt(null, true)).ShouldBeEquivalentTo(longLoopOrder);
    }

    [TestMethod]
    public async Task GetNextNotQueuedHostOrderPickingAt_LongLoopPick_ReturnsHostOrder()
    {
        var pickAt1020 = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var longLoopOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        var longLoopPick = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var fixture = new HostOrderRepositoryTestFixture();
        fixture.Repository.AddRange(pickAt1020, longLoopOrder, longLoopPick);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextNotQueuedHostOrderPickingAt(null, false)).ShouldBeEquivalentTo(longLoopPick);
    }

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
}