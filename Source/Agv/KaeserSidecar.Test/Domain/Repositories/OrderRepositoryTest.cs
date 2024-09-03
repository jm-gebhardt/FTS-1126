using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

[TestClass]
public class OrderRepositoryTest
{
    [TestMethod]
    public void StorePickPOrderReport_HasOrderBeenReported_True()
    {
        var repository = new OrderRepository();
        const string station = "station";
        var order = Guid.NewGuid();

        repository.StorePickOrderReport(station, order);

        repository.HasOrderBeenReported(station, order).ShouldBeTrue();
    }

    [TestMethod]
    public void RemovePickOrderReport_HasOrderBeenReported_False()
    {
        var repository = new OrderRepository();
        const string station = "station";
        var order = Guid.NewGuid();
        repository.StorePickOrderReport(station, order);
        repository.HasOrderBeenReported(station, order).ShouldBeTrue();

        repository.RemovePickOrderReport(station, order);

        repository.HasOrderBeenReported(station, order).ShouldBeFalse();
    }

    [TestMethod]
    public void HasOrderBeenReported_NoSuchStation_False()
    {
        var repository = new OrderRepository();
        var order = Guid.NewGuid();
        repository.StorePickOrderReport("station", order);

        repository.HasOrderBeenReported("different station", order).ShouldBeFalse();
    }

    [TestMethod]
    public void HasOrderBeenReported_OrderNotReported_False()
    {
        var repository = new OrderRepository();
        const string station = "station";
        var order = Guid.NewGuid();
        repository.StorePickOrderReport(station, order);

        repository.HasOrderBeenReported(station, Guid.NewGuid()).ShouldBeFalse();
    }
}