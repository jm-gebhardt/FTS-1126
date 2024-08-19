using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

[TestClass]
public class PrismatCommunicationHostOrderRepositoryTest
{
    [TestMethod]
    public async Task GetNextPickOnShortLoop_ValidData_ReturnsHostOrder()
    {
        var fixture = new PrismatCommunicationHostOrderRepositoryTestFixture();

        var earlierOrder = OrderGenerator.GenerateHostOrderFromOrderDetails( OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var laterOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        fixture.Repository.AddRange(earlierOrder, laterOrder, dropOrder);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextPickOnShortLoop()).ShouldBe(earlierOrder);
    }

    [TestMethod]
    public async Task GetNextPickOnShortLoop_NoOrderPickingAt1020_ReturnsNull()
    {
        var fixture = new PrismatCommunicationHostOrderRepositoryTestFixture();

        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        fixture.Repository.Add(dropOrder);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetNextPickOnShortLoop()).ShouldBeNull();
    }
}