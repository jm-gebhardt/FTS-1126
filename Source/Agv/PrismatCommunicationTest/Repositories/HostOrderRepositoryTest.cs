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
}