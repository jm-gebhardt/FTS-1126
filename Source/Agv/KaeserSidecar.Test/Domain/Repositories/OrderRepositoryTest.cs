using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

[TestClass]
public class OrderRepositoryTest
{
    [TestMethod]
    public async Task StorePickPOrderReport_HasOrderBeenReported_True()
    {
        var repository = new OrderRepositoryTestFixture().Repository;
        const string station = "station";
        var order = Guid.NewGuid();

        await repository.StoreOrderReport(station, order);

       (await repository.HasOrderBeenReported(station, order)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task RemovePickOrderReport_HasOrderBeenReported_False()
    {
        var repository = new OrderRepositoryTestFixture().Repository;
        const string station = "station";
        var order = Guid.NewGuid();
        await repository.StoreOrderReport(station, order);
        (await repository.HasOrderBeenReported(station, order)).ShouldBeTrue();

        await repository.RemoveOrderReport(station, order);

        (await repository.HasOrderBeenReported(station, order)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task HasOrderBeenReported_NoSuchStation_False()
    {
        var repository = new OrderRepositoryTestFixture().Repository;
        var order = Guid.NewGuid();
        await repository.StoreOrderReport("station", order);

        (await repository.HasOrderBeenReported("different station", order)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task HasOrderBeenReported_OrderNotReported_False()
    {
        var repository = new OrderRepositoryTestFixture().Repository;
        const string station = "station";
        var order = Guid.NewGuid();
        await repository.StoreOrderReport(station, order);

        (await repository.HasOrderBeenReported(station, Guid.NewGuid())).ShouldBeFalse();
    }
}