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
        fixture.Repository.Add(dropHostOrder);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.HasDropOrdersHeadingTowardsDecisionPoint()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasDropOrdersHeadingTowardsLoadTriggerPoint_ValidData_ReturnsTrue()
    {
        var fixture = new HostOrderRepositoryTestFixture();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder();
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(order);
        dropHostOrder.Start(DateTime.UtcNow, Guid.NewGuid());

        fixture.Repository.Add(dropHostOrder);
        await fixture.Repository.SaveAsync();

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
}